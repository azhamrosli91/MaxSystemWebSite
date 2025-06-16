namespace MaxSystemWebSite.Models.MCP
{
    public class MCP_EmailRequest
    {
        public string Jsonrpc { get; set; }
        public string Id { get; set; }
        public string Action { get; set; }
        public MCP_EmailParams Params { get; set; }
    }

    public class MCP_EmailParams
    {
        public MCP_EmailEntity Sender { get; set; }
        public List<MCP_EmailEntity> Recipient { get; set; }
        public List<MCP_EmailEntity> Cc { get; set; }
        public List<MCP_EmailEntity> Bcc { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    public class MCP_EmailEntity
    {
        public MCP_EmailAddress EmailAddress { get; set; }
    }

    public class MCP_EmailAddress
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }

}
