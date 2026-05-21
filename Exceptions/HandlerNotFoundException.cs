namespace Venly.Dispatch.Exceptions;

public class HandlerNotFoundException : Exception
{
    public HandlerNotFoundException(string handlerName) 
        : base($"No handler registered for '{handlerName}'")
    { }
}