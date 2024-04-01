namespace ErrorCodeSourceGeneratorTest.Errors;

public class Error
{
    public string Code { get; private set; }
    public string Message { get; private set; }
    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public Error FormatWith(params object[] parameters)
    {
        Message = string.Format(Message, parameters);
        return this;
    }
}
