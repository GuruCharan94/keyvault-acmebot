﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using ACMESharp.Authorizations;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;

using Azure;
using Azure.Security.KeyVault.Certificates;

using DnsClient;

using KeyVault.Acmebot.Internal;
using KeyVault.Acmebot.Models;
using KeyVault.Acmebot.Options;
using KeyVault.Acmebot.Providers;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

namespace KeyVault.Acmebot.Functions;

public class SharedActivity : ISharedActivity
{
    public SharedActivity(LookupClient lookupClient, AcmeProtocolClientFactory acmeProtocolClientFactory,
                          IEnumerable<IDnsProvider> dnsProviders, CertificateClient certificateClient,
                          WebhookInvoker webhookInvoker, IOptions<AcmebotOptions> options, ILogger<SharedActivity> logger,
                          CertificateRenewalEligibilityChecker certificateRenewalEligibilityChecker)
    {
        _lookupClient = lookupClient;
        _acmeProtocolClientFactory = acmeProtocolClientFactory;
        _dnsProviders = dnsProviders;
        _certificateClient = certificateClient;
        _webhookInvoker = webhookInvoker;
        _options = options.Value;
        _logger = logger;
        _certificateRenewalEligibilityChecker = certificateRenewalEligibilityChecker;
    }

    private readonly LookupClient _lookupClient;
    private readonly AcmeProtocolClientFactory _acmeProtocolClientFactory;
    private readonly IEnumerable<IDnsProvider> _dnsProviders;
    private readonly CertificateClient _certificateClient;
    private readonly WebhookInvoker _webhookInvoker;
    private readonly AcmebotOptions _options;
    private readonly ILogger<SharedActivity> _logger;
    private readonly CertificateRenewalEligibilityChecker _certificateRenewalEligibilityChecker;

    [FunctionName(nameof(GetExpiringCertificates))]
    public async Task<IReadOnlyList<CertificateItem>> GetExpiringCertificates([ActivityTrigger] DateTime currentDateTime)
    {
        var certificates = _certificateClient.GetPropertiesOfCertificatesAsync();
        var result = new List<CertificateItem>();

        // Initialize ARI client if enabled
        var acmeProtocolClient = await _acmeProtocolClientFactory.CreateClientAsync();

        await foreach (var certificate in certificates)
        {
            if (!certificate.IsIssuedByAcmebot() || !certificate.IsSameEndpoint(_options.Endpoint))
            {
                continue;
            }

            var certificateWithPolicy = (await _certificateClient.GetCertificateAsync(certificate.Name)).Value;

            var shouldRenew = await _certificateRenewalEligibilityChecker.IsCertificateDueForRenewalAsync(
                certificateWithPolicy, currentDateTime, acmeProtocolClient?.Directory?.RenewalInfo);

            if (shouldRenew)
            {
                result.Add(certificateWithPolicy.ToCertificateItem());
            }
        }
        return result;
    }

    [FunctionName(nameof(GetAllCertificates))]
    public async Task<IReadOnlyList<CertificateItem>> GetAllCertificates([ActivityTrigger] object input = null)
    {
        var certificates = _certificateClient.GetPropertiesOfCertificatesAsync();

        var result = new List<CertificateItem>();

        await foreach (var certificate in certificates)
        {
            var certificateItem = (await _certificateClient.GetCertificateAsync(certificate.Name)).Value.ToCertificateItem();

            certificateItem.IsIssuedByAcmebot = certificate.IsIssuedByAcmebot();
            certificateItem.IsSameEndpoint = certificate.IsSameEndpoint(_options.Endpoint);

            result.Add(certificateItem);
        }

        return result;
    }

    [FunctionName(nameof(GetAllDnsZones))]
    public async Task<IReadOnlyList<DnsZoneGroup>> GetAllDnsZones([ActivityTrigger] object input = null)
    {
        try
        {
            var zones = await _dnsProviders.ListAllZonesAsync();

            return zones.Select(x => new DnsZoneGroup
            {
                DnsProviderName = x.Item1,
                DnsZones = x.Item2?.Select(xs => xs.ToDnsZoneItem()).OrderBy(xs => xs.Name).ToArray()
            }).ToArray();
        }
        catch
        {
            return Array.Empty<DnsZoneGroup>();
        }
    }

    [FunctionName(nameof(GetCertificatePolicy))]
    public async Task<CertificatePolicyItem> GetCertificatePolicy([ActivityTrigger] string certificateName)
    {
        KeyVaultCertificateWithPolicy certificate = await _certificateClient.GetCertificateAsync(certificateName);

        return certificate.ToCertificatePolicyItem();
    }

    [FunctionName(nameof(RevokeCertificate))]
    public async Task RevokeCertificate([ActivityTrigger] string certificateName)
    {
        var response = await _certificateClient.GetCertificateAsync(certificateName);

        var acmeProtocolClient = await _acmeProtocolClientFactory.CreateClientAsync();

        await acmeProtocolClient.RevokeCertificateAsync(response.Value.Cer);
    }

    [FunctionName(nameof(Order))]
    public async Task<OrderDetails> Order([ActivityTrigger] CertificatePolicyItem certificatePolicyItem)
    {
        var dnsNames = certificatePolicyItem.DnsNames;
        var existingCertificateName = certificatePolicyItem.CertificateName;
        var acmeProtocolClient = await _acmeProtocolClientFactory.CreateClientAsync();
        string replacesCertId = null;

        if (_options.AriEnabled)
        {
            try
            {
                // Attempt to get existing certificate for ARI replacement ID
                var certificateForAri = await _certificateClient.GetCertificateAsync(existingCertificateName);
                replacesCertId = certificateForAri.Value.ExtractARICertificateId();
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Cert does not exist in KV. New Order
                _logger.LogInformation("Certificate {CertificateName} does not exist yet - creating new certificate", existingCertificateName);
            }
            catch (Exception ex)
            {
                // Cert Exists but ARI extraction failed. For now, let's fallback to ordering without ARI.
                _logger.LogWarning(ex, "Failed to extract ARI certificate ID for {CertificateName}, ordering without ARI", existingCertificateName);
            }
        }

        return await acmeProtocolClient.CreateOrderAsync(
            dnsNames, replacesCertId, null, null, CancellationToken.None);
    }

    [FunctionName(nameof(Dns01Precondition))]
    public async Task<string> Dns01Precondition([ActivityTrigger] CertificatePolicyItem certificatePolicyItem)
    {
        // DNS zone の一覧を各 Provider から取得
        var zones = await _dnsProviders.FlattenAllZonesAsync();

        // DNS zone が存在するか確認
        var foundZones = new HashSet<DnsZone>();
        var notFoundZoneDnsNames = new List<string>();

        foreach (var dnsName in certificatePolicyItem.AliasedDnsNames)
        {
            var zone = zones.FindDnsZone(dnsName);

            // マッチする DNS zone が見つからない場合はエラー
            if (zone is null)
            {
                notFoundZoneDnsNames.Add(dnsName);
                continue;
            }

            foundZones.Add(zone);
        }

        if (notFoundZoneDnsNames.Count > 0)
        {
            throw new PreconditionException($"DNS zone(s) are not found. DnsNames = {string.Join(",", notFoundZoneDnsNames)}");
        }

        // DNS zone に移譲されている Name servers が正しいか検証
        foreach (var zone in foundZones.Where(x => x.NameServers is { Count: > 0 }))
        {
            // DNS provider が Name servers を返している場合は NS レコードを確認
            var queryResult = await _lookupClient.QueryAsync(zone.Name, QueryType.NS);

            // 最後の . が付いている場合があるので削除して統一
            var expectedNameServers = zone.NameServers
                                          .Select(x => x.TrimEnd('.'))
                                          .ToArray();

            var actualNameServers = queryResult.Answers
                                               .OfType<DnsClient.Protocol.NsRecord>()
                                               .Select(x => x.NSDName.Value.TrimEnd('.'))
                                               .ToArray();

            // 処理対象の DNS zone から取得した NS と実際に引いた NS の値が一つも一致しない場合はエラー
            if (!actualNameServers.Intersect(expectedNameServers, StringComparer.OrdinalIgnoreCase).Any())
            {
                throw new PreconditionException($"The delegated name server is not correct. DNS zone = {zone.Name}, Expected = {string.Join(",", expectedNameServers)}, Actual = {string.Join(",", actualNameServers)}");
            }
        }

        // 指定された DNS Provider に属する DNS zone を優先する
        var dnsProvider = foundZones.Select(x => x.DnsProvider).FirstOrDefault(x => x.Name == certificatePolicyItem.DnsProviderName);

        // DNS zone の属する Provider が変わった可能性があるのでフォールバック
        if (dnsProvider is null)
        {
            // 見つかった DNS zone の属する DNS Provider を取得
            var dnsProviders = foundZones.Select(x => x.DnsProvider).DistinctBy(x => x.Name).ToArray();

            // 単一の DNS Provider で構成された証明書かチェックする
            if (dnsProviders.Length != 1)
            {
                // 互換性のために常に空文字列を返す
                return "";
            }

            // 単一の DNS Provider で構成されている場合は問題ない
            dnsProvider = dnsProviders.First();
        }

        return dnsProvider.Name;
    }

    [FunctionName(nameof(Dns01Authorization))]
    public async Task<(IReadOnlyList<AcmeChallengeResult>, int)> Dns01Authorization([ActivityTrigger] (string, string, IReadOnlyList<string>) input)
    {
        var (dnsProviderName, dnsAlias, authorizationUrls) = input;

        var acmeProtocolClient = await _acmeProtocolClientFactory.CreateClientAsync();

        var challengeResults = new List<AcmeChallengeResult>();

        foreach (var authorizationUrl in authorizationUrls)
        {
            // Authorization の詳細を取得
            var authorization = await acmeProtocolClient.GetAuthorizationDetailsAsync(authorizationUrl);

            // ignore authorizations that are already valid 
            if (authorization.Status == "valid")
            {
                continue;
            }

            // DNS-01 Challenge の情報を拾う
            var challenge = authorization.Challenges.FirstOrDefault(x => x.Type == "dns-01");

            if (challenge is null)
            {
                throw new PreconditionException("DNS-01 cannot be used for domains for which a certificate has already been issued using HTTP-01.");
            }

            var challengeValidationDetails = AuthorizationDecoder.ResolveChallengeForDns01(authorization, challenge, acmeProtocolClient.Signer);

            // Challenge の情報を保存する
            challengeResults.Add(new AcmeChallengeResult
            {
                Url = challenge.Url,
                DnsRecordName = string.IsNullOrEmpty(dnsAlias) ? challengeValidationDetails.DnsRecordName : $"_acme-challenge.{dnsAlias}",
                DnsRecordValue = challengeValidationDetails.DnsRecordValue
            });
        }

        // DNS zone の一覧を各 Provider から取得
        var zones = (await (string.IsNullOrEmpty(dnsProviderName) ? _dnsProviders.FlattenAllZonesAsync() : _dnsProviders.ListZonesAsync(dnsProviderName)));

        var propagationSeconds = 0;

        // DNS-01 の検証レコード名毎に DNS に TXT レコードを作成
        foreach (var lookup in challengeResults.ToLookup(x => x.DnsRecordName))
        {
            var dnsRecordName = lookup.Key;

            var zone = zones.FindDnsZone(dnsRecordName);

            if (zone is null)
            {
                throw new PreconditionException($"DNS zone is not found. DnsRecordName = {dnsRecordName}");
            }

            // Challenge の詳細から DNS 向けにレコード名を作成
            var acmeDnsRecordName = dnsRecordName.Replace($".{zone.Name}", "", StringComparison.OrdinalIgnoreCase);

            await zone.DnsProvider.DeleteTxtRecordAsync(zone, acmeDnsRecordName);
            await zone.DnsProvider.CreateTxtRecordAsync(zone, acmeDnsRecordName, lookup.Select(x => x.DnsRecordValue));

            // 一番時間のかかる DNS Provider に合わせる
            propagationSeconds = Math.Max(propagationSeconds, zone.DnsProvider.PropagationSeconds);
        }

        return (challengeResults, propagationSeconds);
    }

    [FunctionName(nameof(CheckDnsChallenge))]
    public async Task CheckDnsChallenge([ActivityTrigger] IReadOnlyList<AcmeChallengeResult> challengeResults)
    {
        foreach (var challengeResult in challengeResults)
        {
            IDnsQueryResponse queryResult;

            try
            {
                // 実際に ACME の TXT レコードを引いて確認する
                queryResult = await _lookupClient.QueryAsync(challengeResult.DnsRecordName, QueryType.TXT);
            }
            catch (DnsResponseException ex)
            {
                // 一時的な DNS エラーの可能性があるためリトライ
                throw new RetriableActivityException($"{challengeResult.DnsRecordName} bad response. Message: \"{ex.DnsError}\"", ex);
            }

            var txtRecords = queryResult.Answers
                                        .OfType<DnsClient.Protocol.TxtRecord>()
                                        .ToArray();

            // レコードが存在しなかった場合はエラー
            if (txtRecords.Length == 0)
            {
                throw new RetriableActivityException($"{challengeResult.DnsRecordName} did not resolve.");
            }

            // レコードに今回のチャレンジが含まれていない場合もエラー
            if (!txtRecords.Any(x => x.Text.Contains(challengeResult.DnsRecordValue)))
            {
                throw new RetriableActivityException($"{challengeResult.DnsRecordName} is not correct. Expected: \"{challengeResult.DnsRecordValue}\", Actual: \"{string.Join(",", txtRecords.SelectMany(x => x.Text))}\"");
            }
        }
    }

    [FunctionName(nameof(AnswerChallenges))]
    public async Task AnswerChallenges([ActivityTrigger] IReadOnlyList<AcmeChallengeResult> challengeResults)
    {
        var acmeProtocolClient = await _acmeProtocolClientFactory.CreateClientAsync();

        // Answer の準備が出来たことを通知
        foreach (var challengeResult in challengeResults)
        {
            await acmeProtocolClient.AnswerChallengeAsync(challengeResult.Url);
        }
    }

    [FunctionName(nameof(CheckIsReady))]
    public async Task CheckIsReady([ActivityTrigger] (OrderDetails, IReadOnlyList<AcmeChallengeResult>) input)
    {
        var (orderDetails, challengeResults) = input;

        var acmeProtocolClient = await _acmeProtocolClientFactory.CreateClientAsync();

        orderDetails = await acmeProtocolClient.GetOrderDetailsAsync(orderDetails.OrderUrl, orderDetails);

        if (orderDetails.Payload.Status == "invalid")
        {
            var problems = new List<Problem>();

            foreach (var challengeResult in challengeResults)
            {
                var challenge = await acmeProtocolClient.GetChallengeDetailsAsync(challengeResult.Url);

                if (challenge.Status != "invalid" || challenge.Error is null)
                {
                    continue;
                }

                _logger.LogError($"ACME domain validation error: {JsonConvert.SerializeObject(challenge.Error)}");

                problems.Add(challenge.Error);
            }

            // 全てのエラーが dns 関係の場合は Orchestrator からリトライさせる
            if (problems.All(x => x.Type == "urn:ietf:params:acme:error:dns"))
            {
                throw new RetriableOrchestratorException("ACME validation status is invalid, but retriable error. It will retry automatically.");
            }

            // invalid の場合は最初から実行が必要なので失敗させる
            throw new InvalidOperationException($"ACME validation status is invalid. Required retry at first.\nLastError = {JsonConvert.SerializeObject(problems.Last())}");
        }

        if (orderDetails.Payload.Status != "ready")
        {
            // ready 以外の場合はリトライする
            throw new RetriableActivityException($"ACME validation status is {orderDetails.Payload.Status}. It will retry automatically.");
        }
    }

    [FunctionName(nameof(FinalizeOrder))]
    public async Task<OrderDetails> FinalizeOrder([ActivityTrigger] (CertificatePolicyItem, OrderDetails) input)
    {
        var (certificatePolicyItem, orderDetails) = input;

        byte[] csr;

        try
        {
            var certificatePolicy = certificatePolicyItem.ToCertificatePolicy();
            var metadata = certificatePolicyItem.ToCertificateMetadata(_options.Endpoint);

            var certificateOperation = await _certificateClient.StartCreateCertificateAsync(certificatePolicyItem.CertificateName, certificatePolicy, tags: metadata);

            csr = certificateOperation.Properties.Csr;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Conflict)
        {
            var certificateOperation = await _certificateClient.GetCertificateOperationAsync(certificatePolicyItem.CertificateName);

            csr = certificateOperation.Properties.Csr;
        }

        // Order の最終処理を実行する
        var acmeProtocolClient = await _acmeProtocolClientFactory.CreateClientAsync();

        return await acmeProtocolClient.FinalizeOrderAsync(orderDetails.Payload.Finalize, csr);
    }

    [FunctionName(nameof(CheckIsValid))]
    public async Task<OrderDetails> CheckIsValid([ActivityTrigger] OrderDetails orderDetails)
    {
        var acmeProtocolClient = await _acmeProtocolClientFactory.CreateClientAsync();

        orderDetails = await acmeProtocolClient.GetOrderDetailsAsync(orderDetails.OrderUrl, orderDetails);

        if (orderDetails.Payload.Status == "invalid")
        {
            // invalid の場合は最初から実行が必要なので失敗させる
            throw new InvalidOperationException("Finalize request is invalid. Required retry at first.");
        }

        if (orderDetails.Payload.Status != "valid")
        {
            // valid 以外の場合はリトライする
            throw new RetriableActivityException($"Finalize request is {orderDetails.Payload.Status}. It will retry automatically.");
        }

        return orderDetails;
    }

    [FunctionName(nameof(MergeCertificate))]
    public async Task<CertificateItem> MergeCertificate([ActivityTrigger] (string, OrderDetails) input)
    {
        var (certificateName, orderDetails) = input;

        var acmeProtocolClient = await _acmeProtocolClientFactory.CreateClientAsync();

        // 証明書をダウンロードして Key Vault へ格納
        var x509Certificates = await acmeProtocolClient.GetOrderCertificateAsync(orderDetails, _options.PreferredChain);

        var exportedX509Certificates = x509Certificates.Select(x => x.Export(X509ContentType.Pfx));

        var mergeCertificateOptions = new MergeCertificateOptions(
            certificateName,
            _options.MitigateChainOrder ? exportedX509Certificates.Reverse() : exportedX509Certificates
        );

        return (await _certificateClient.MergeCertificateAsync(mergeCertificateOptions)).Value.ToCertificateItem();
    }

    [FunctionName(nameof(CleanupDnsChallenge))]
    public async Task CleanupDnsChallenge([ActivityTrigger] (string, IReadOnlyList<AcmeChallengeResult>) input)
    {
        var (dnsProviderName, challengeResults) = input;

        // DNS zone の一覧を各 Provider から取得
        var zones = (await (string.IsNullOrEmpty(dnsProviderName) ? _dnsProviders.FlattenAllZonesAsync() : _dnsProviders.ListZonesAsync(dnsProviderName)));

        // DNS-01 の検証レコード名毎に DNS から TXT レコードを削除
        foreach (var lookup in challengeResults.ToLookup(x => x.DnsRecordName))
        {
            var dnsRecordName = lookup.Key;

            var zone = zones.FindDnsZone(dnsRecordName);

            // Challenge の詳細から DNS 向けにレコード名を作成
            var acmeDnsRecordName = dnsRecordName.Replace($".{zone.Name}", "", StringComparison.OrdinalIgnoreCase);

            await zone.DnsProvider.DeleteTxtRecordAsync(zone, acmeDnsRecordName);
        }
    }

    [FunctionName(nameof(SendCompletedEvent))]
    public Task SendCompletedEvent([ActivityTrigger] (string, DateTimeOffset?, IReadOnlyList<string>) input)
    {
        var (certificateName, expirationDate, dnsNames) = input;

        return _webhookInvoker.SendCompletedEventAsync(certificateName, expirationDate, dnsNames, _options.Endpoint.Host);
    }



}
