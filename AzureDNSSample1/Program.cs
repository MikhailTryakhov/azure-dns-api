using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;

// Azure Management dependencies
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Details for subscription and Service Principal account
            // This demo assumes the Service Principal account uses password-based authentication (certificate-based is also possible)
            // See https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal/ for details
            var tenantId = "<Your tenant ID>";
            var clientId = "<Your client ID>";
            var secret = "<Your service account pwd>";
            var subscriptionId = "<Your subscription ID>";

            // The resource group which this this sample will use.
            // This needs to exist already, and the Service Principal needs to have been granted 'DNS Zone Contributor' permissions to the resource group
            var resourceGroupName = "<Your resource group name>";

            // The DNS zone name which this sample will use.
            // Does not need to exist, will be created and deleted by this sample.
            var zoneName = "<Your sample zone name>";

            RunSample(tenantId, clientId, secret, subscriptionId, resourceGroupName, zoneName).Wait();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        public static async Task RunSample(string tenantId, string clientId, string secret, string subscriptionId, string resourceGroupName, string zoneName)
        {
            // Build the service credentials and DNS management client
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, secret);
            var dnsClient = new DnsManagementClient(serviceCreds);
            dnsClient.SubscriptionId = subscriptionId;

            #region Create DNS Zone
            // **********************************************************************************************************
            // Create DNS Zone
            // **********************************************************************************************************

            Console.Write("Creating DNS zone '{0}'...", zoneName);
            try
            {
                // Create zone parameters
                var dnsZoneParams = new Zone("global"); // All DNS zones must have location = "global"
                // Create a Azure Resource Manager 'tag'.  This is optional.  You can add multiple tags
                dnsZoneParams.Tags = new Dictionary<string, string>();
                dnsZoneParams.Tags.Add("dept", "finance");

                // Create the actual zone.
                // Note: Uses 'If-None-Match *' ETAG check, so will fail if the zone exists already.
                // Note: For non-async usage, call dnsClient.Zones.CreateOrUpdate(resourceGroupName, zoneName, dnsZoneParams, null, "*")
                // Note: For getting the http response, call dnsClient.Zones.CreateOrUpdateWithHttpMessagesAsync(resourceGroupName, zoneName, dnsZoneParams, null, "*")
                var dnsZone = await dnsClient.Zones.CreateOrUpdateAsync(resourceGroupName, zoneName, dnsZoneParams, null, "*");

                Console.WriteLine("success");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
            #endregion

            #region Create A Record
            // **********************************************************************************************************
            // Create A Record
            // **********************************************************************************************************

            var recordSetName = "www";
            Console.Write("Creating DNS 'A' record set with name '{0}'...", recordSetName);
            try
            {
                // Create record set parameters
                var recordSetParams = new RecordSet();
                recordSetParams.TTL = 3600;

                // Add records to the record set parameter object.  In this case, we'll add a record of type 'A'
                recordSetParams.ARecords = new List<ARecord>();
                recordSetParams.ARecords.Add(new ARecord("1.2.3.4"));

                // Add metadata to the record set.  Similar to Azure Resource Manager tags, this is optional and you can add multiple metadata name/value pairs
                recordSetParams.Metadata = new Dictionary<string, string>();
                recordSetParams.Metadata.Add("user", "Mary");

                // Create the actual record set in Azure DNS
                // Note: no ETAG checks specified, will overwrite existing record set if one exists
                var recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(resourceGroupName, zoneName, recordSetName, RecordType.A, recordSetParams);

                Console.WriteLine("success");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
            #endregion

            #region Update A Record
            // **********************************************************************************************************
            // Update A Record
            // **********************************************************************************************************

            Console.Write("Updating DNS 'A' record set with name '{0}'...", recordSetName);
            try
            {
                var recordSet = dnsClient.RecordSets.Get(resourceGroupName, zoneName, recordSetName, RecordType.A);

                // Add a new record to the local object.  Note that records in a record set must be unique/distinct
                recordSet.ARecords.Add(new ARecord("5.6.7.8"));

                // Update the record set in Azure DNS
                // Note: ETAG check specified, update will be rejected if the record set has changed in the meantime
                recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(resourceGroupName, zoneName, recordSetName, RecordType.A, recordSet, recordSet.Etag);

                Console.WriteLine("success");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
            #endregion

            #region Delete A Record
            // **********************************************************************************************************
            // Delete A Record
            // **********************************************************************************************************

            Console.Write("Deleting DNS 'A' record set with name '{0}'...", recordSetName);
            try
            {
                // Delete the record set
                // Note: No ETAG checks, record set will be deleted regardless of concurrent changes
                dnsClient.RecordSets.Delete(resourceGroupName, zoneName, recordSetName, RecordType.A);

                Console.WriteLine("success");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
            #endregion

            #region Create AAAA Record
            // **********************************************************************************************************
            // Create AAAA Record
            // **********************************************************************************************************

            recordSetName = "aaaa-test";
            Console.Write("Creating DNS 'AAAA' record set with name '{0}'...", recordSetName);
            try
            {
                // Create record set parameters
                var recordSetParams = new RecordSet();
                recordSetParams.TTL = 3600;

                // Add records to the record set parameter object.
                recordSetParams.AaaaRecords = new List<AaaaRecord>();
                recordSetParams.AaaaRecords.Add(new AaaaRecord("aaaa:4444:33:2::11"));

                // Create the actual record set in Azure DNS
                // Note: no ETAG checks specified, will overwrite existing record set if one exists
                var recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(resourceGroupName, zoneName, recordSetName, RecordType.AAAA, recordSetParams);

                Console.WriteLine("success");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
            #endregion

            #region Create CNAME Record
            // **********************************************************************************************************
            // Create CNAME Record
            // **********************************************************************************************************

            recordSetName = "cname-test";
            Console.Write("Creating DNS 'CNAME' record set with name '{0}'...", recordSetName);
            try
            {
                // Create record set parameters
                var recordSetParams = new RecordSet();
                recordSetParams.TTL = 3600;

                // Add record to the record set parameter object.  In the CNAME case, there's just one CNAME record, not a list.
                recordSetParams.CnameRecord = new CnameRecord("www.contoso.com");

                // Create the actual record set in Azure DNS
                // Note: no ETAG checks specified, will overwrite existing record set if one exists
                var recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(resourceGroupName, zoneName, recordSetName, RecordType.CNAME, recordSetParams);

                Console.WriteLine("success");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
            #endregion

            #region Create MX Record
            // **********************************************************************************************************
            // Create MX Record
            // **********************************************************************************************************

            recordSetName = "mx-test";
            Console.Write("Creating DNS 'MX' record set with name '{0}'...", recordSetName);
            try
            {
                // Create record set parameters
                var recordSetParams = new RecordSet();
                recordSetParams.TTL = 3600;

                // Add record to the record set parameter object. 
                recordSetParams.MxRecords = new List<MxRecord>();
                recordSetParams.MxRecords.Add(new MxRecord(10, "mail.contoso.com"));

                // Create the actual record set in Azure DNS
                // Note: no ETAG checks specified, will overwrite existing record set if one exists
                var recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(resourceGroupName, zoneName, recordSetName, RecordType.MX, recordSetParams);

                Console.WriteLine("success");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
            #endregion

            #region Create NS Record
            // **********************************************************************************************************
            // Create NS Record
            // **********************************************************************************************************

            recordSetName = "ns-test";
            Console.Write("Creating DNS 'NS' record set with name '{0}'...", recordSetName);
            try
            {
                // Create record set parameters
                var recordSetParams = new RecordSet();
                recordSetParams.TTL = 3600;

                // Add record to the record set parameter object. 
                recordSetParams.NsRecords = new List<NsRecord>();
                recordSetParams.NsRecords.Add(new NsRecord("ns1.contoso.com"));

                // Create the actual record set in Azure DNS
                // Note: no ETAG checks specified, will overwrite existing record set if one exists
                var recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(resourceGroupName, zoneName, recordSetName, RecordType.NS, recordSetParams);

                Console.WriteLine("success");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
            #endregion

            #region Create PTR Record
            // **********************************************************************************************************
            // Create PTR Record
            // Note: Normally we'd create a PTR record with a name mapping to an IP address in an ARPA zone.
            //       For the purposes of this demo, we'll just use the existing zone
            // **********************************************************************************************************

            recordSetName = "ptr-test";
            Console.Write("Creating DNS 'PTR' record set with name '{0}'...", recordSetName);
            try
            {
                // Create record set parameters
                var recordSetParams = new RecordSet();
                recordSetParams.TTL = 3600;

                // Add record to the record set parameter object. 
                recordSetParams.PtrRecords = new List<PtrRecord>();
                recordSetParams.PtrRecords.Add(new PtrRecord("ptr.contoso.com"));

                // Create the actual record set in Azure DNS
                // Note: no ETAG checks specified, will overwrite existing record set if one exists
                var recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(resourceGroupName, zoneName, recordSetName, RecordType.PTR, recordSetParams);

                Console.WriteLine("success");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
            #endregion

            #region Create SRV Record
            // **********************************************************************************************************
            // Create SRV Record
            // **********************************************************************************************************

            // Note: Service and Protocol are part of the record set name
            recordSetName = "_sip._tcp";
            Console.Write("Creating DNS 'SRV' record set with name '{0}'...", recordSetName);
            try
            {
                // Create record set parameters
                var recordSetParams = new RecordSet();
                recordSetParams.TTL = 3600;

                // Add record to the record set parameter object. 
                recordSetParams.SrvRecords = new List<SrvRecord>();
                recordSetParams.SrvRecords.Add(new SrvRecord(1, 10, 80, "srv.contoso.com"));

                // Create the actual record set in Azure DNS
                // Note: no ETAG checks specified, will overwrite existing record set if one exists
                var recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(resourceGroupName, zoneName, recordSetName, RecordType.SRV, recordSetParams);

                Console.WriteLine("success");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
            #endregion

            #region Create TXT Record
            // **********************************************************************************************************
            // Create TXT Record
            // **********************************************************************************************************

            // Note: Service and Protocol are part of the record set name
            recordSetName = "txt-test";
            Console.Write("Creating DNS 'TXT' record set with name '{0}'...", recordSetName);
            try
            {
                // Create record set parameters
                var recordSetParams = new RecordSet();
                recordSetParams.TTL = 3600;

                // Add record to the record set parameter object. 
                // For each record, create a list of strings, max 255 characters per string (clients concatenate the strings within each record and treat as a single string)
                recordSetParams.TxtRecords = new List<TxtRecord>();
                var strings = new List<string>();
                strings.Add("this is the first string in the first record in the record set");
                strings.Add("this is the second string in the first record in the record set");
                recordSetParams.TxtRecords.Add(new TxtRecord(strings));

                // Create the actual record set in Azure DNS
                // Note: no ETAG checks specified, will overwrite existing record set if one exists
                var recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(resourceGroupName, zoneName, recordSetName, RecordType.TXT, recordSetParams);

                Console.WriteLine("success");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
            #endregion

            #region List Record Sets - all record types
            // **********************************************************************************************************
            // List Record Sets - all record types
            // **********************************************************************************************************

            Console.Write("Counting record sets...");
            int recordSets = 0;
            try
            {
                // Note: in this demo, we'll use a very small page size (2 record sets) to demonstrate paging
                // In practice, to improve performance you would use a large page size or just use the system default
                var page = await dnsClient.RecordSets.ListAllInResourceGroupAsync(resourceGroupName, zoneName, "2");
                recordSets += page.Count();

                while (page.NextPageLink != null)
                {
                    page = await dnsClient.RecordSets.ListAllInResourceGroupNextAsync(page.NextPageLink);
                    recordSets += page.Count();
                }

                Console.WriteLine("success, {0} record sets found", recordSets);
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
            #endregion

            #region List Record Sets - NS record type
            // **********************************************************************************************************
            // List Record Sets - NS record type
            // **********************************************************************************************************

            Console.Write("Counting NS record sets...");
            recordSets = 0;
            try
            {
                // Note: in this demo, we'll use a very small page size (2 record sets) to demonstrate paging
                // In practice, to improve performance you would use a large page size or just use the system default
                var page = await dnsClient.RecordSets.ListByTypeAsync(resourceGroupName, zoneName, RecordType.NS, "2");
                recordSets += page.Count();

                while (page.NextPageLink != null)
                {
                    page = await dnsClient.RecordSets.ListByTypeNextAsync(page.NextPageLink);
                    recordSets += page.Count();
                }

                Console.WriteLine("success, {0} NS record sets found", recordSets);
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
            #endregion

            #region Delete DNS Zone
            // **********************************************************************************************************
            // Delete DNS Zone
            // **********************************************************************************************************

            Console.Write("Deleting DNS zone '{0}'...", zoneName);
            try
            {
                // Delete the zone.  Will also delete all record sets in the zone.
                await dnsClient.Zones.DeleteAsync(resourceGroupName, zoneName);

                Console.WriteLine("success");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
            #endregion
        }

    }
}

