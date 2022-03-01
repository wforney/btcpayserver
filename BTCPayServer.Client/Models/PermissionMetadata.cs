using Newtonsoft.Json;

namespace BTCPayServer.Client.Models;

public class PermissionMetadata
{
    static PermissionMetadata()
    {
        Dictionary<string, PermissionMetadata> nodes = new Dictionary<string, PermissionMetadata>();
        foreach (var policy in Client.Policies.AllPolicies)
        {
            nodes.Add(policy, new PermissionMetadata() { PermissionName = policy });
        }
        foreach (KeyValuePair<string, PermissionMetadata> n in nodes)
        {
            foreach (var policy in Client.Policies.AllPolicies)
            {
                if (policy.Equals(n.Key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Client.Permission.Create(n.Key).Contains(Client.Permission.Create(policy)))
                {
                    n.Value.SubPermissions.Add(policy);
                }
            }
        }
        foreach (KeyValuePair<string, PermissionMetadata> n in nodes)
        {
            n.Value.SubPermissions.Sort();
        }
        PermissionNodes = nodes.Values.OrderBy(v => v.PermissionName).ToArray();
    }
    public static readonly PermissionMetadata[] PermissionNodes;
    [JsonProperty("name")]
    public string PermissionName { get; set; }
    [JsonProperty("included")]
    public List<string> SubPermissions { get; set; } = new List<string>();
}
