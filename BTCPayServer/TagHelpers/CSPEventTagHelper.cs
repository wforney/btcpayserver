using BTCPayServer.Security;
using Microsoft.AspNetCore.Razor.TagHelpers;
namespace BTCPayServer.TagHelpers;


/// <summary>
/// Add 'unsafe-hashes' and sha256- to allow inline event handlers in CSP
/// </summary>
[HtmlTargetElement(Attributes = "onclick")]
[HtmlTargetElement(Attributes = "onkeypress")]
[HtmlTargetElement(Attributes = "onchange")]
[HtmlTargetElement(Attributes = "onsubmit")]
public class CSPEventTagHelper : TagHelper
{
    public const string EventNames = "onclick,onkeypress,onchange,onsubmit";
    private readonly ContentSecurityPolicies _csp;
    private static readonly HashSet<string> EventSet = EventNames.Split(',')
                                                .ToHashSet();
    public CSPEventTagHelper(ContentSecurityPolicies csp)
    {
        _csp = csp;
    }
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        foreach (TagHelperAttribute attr in output.Attributes)
        {
            var n = attr.Name.ToLowerInvariant();
            if (EventSet.Contains(n))
            {
                _csp.AllowUnsafeHashes(attr.Value.ToString());
            }
        }
    }
}
