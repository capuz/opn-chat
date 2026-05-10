namespace opn_chat.Domain.Entities
{
    public class CommandPermission
    {
        public string CommandName   { get; set; } = "";
        public string Description   { get; set; } = "";
        public string Syntax        { get; set; } = "";
        public string Category      { get; set; } = "";
        public string Examples      { get; set; } = ""; // semicolon-separated
        public bool MemberAllowed   { get; set; }
        public bool OperatorAllowed { get; set; }
        public bool FounderAllowed  { get; set; }
        public bool AdminAllowed    { get; set; }
        public bool IsDangerous     { get; set; }
        public bool IsSystem        { get; set; }
        public bool IsDeprecated    { get; set; }
    }
}
