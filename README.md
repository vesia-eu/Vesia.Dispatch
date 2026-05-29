# What is Vesia.Dispatch?
Vesia.Dispatch is a free, open-source alternative to MediatR built for CQRS patterns and Clean Architecture.

There are 3 types that can be dispatched — Commands, Queries, and Notifications.

Logging is built in and flexible. You can log all commands and queries, disable logging entirely, or use Opt-In mode to only log types marked with `[Logged]`. Commands and queries are configured independently.

Vesia.Dispatch uses the standard `Microsoft.Extensions.Logging` abstraction — plug in any logging provider you prefer (Serilog, NLog, etc.) and it works automatically.


# The types
| Type | Returns | Handlers | Use case |
|------|---------|----------|----------|
| Command | `TResult` | Exactly one | Write operations, state changes |
| Query | `TResult` | Exactly one | Read operations, data retrieval |
| Notification | Nothing | Zero or more | Domain events, fan-out |


# Getting started

## Installation
```bash
dotnet add package Vesia.Dispatch
```

### Setup
```csharp
services.AddDispatch(options =>
{
    options.CommandLogging = LoggingMode.All;
    options.QueryLogging = LoggingMode.OptIn;
});
```

### Commands
```csharp
// Define a command
public record CreateUserCommand(string Name) : ICommand;

// Define a handler
public class CreateUserCommandHandler : ICommandHandler
{
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var user = new User(command.Name);
        return UserDto.From(user);
    }
}

// Dispatch it
var user = await dispatcher.DispatchAsync(new CreateUserCommand("Oliver"));
```

### Queries
```csharp
// Define a query
public record GetUserQuery(Guid Id) : IQuery;

// Define a handler
public class GetUserQueryHandler : IQueryHandler
{
    public async Task Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        // fetch and return user
    }
}

// Dispatch it
var user = await dispatcher.DispatchAsync(new GetUserQuery(id));
```

### Notifications
```csharp
// Define a notification
public record UserCreatedEvent(Guid UserId) : INotification;

// Define a handler (multiple allowed)
public class SendWelcomeEmailHandler : INotificationHandler
{
    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        // send welcome email
    }
}

// Publish it
await dispatcher.PublishAsync(new UserCreatedEvent(user.Id));
```

### Opt-In Logging
```csharp
// Mark specific queries for logging when using LoggingMode.OptIn
[Logged]
public record GetUserQuery(Guid Id) : IQuery;
```

### Custom Pipeline Behaviors
You can add your own pipeline behaviors for cross-cutting concerns like validation or transactions.

```csharp
public class ValidationBehavior : ICommandPipelineBehavior<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand command, Func<Task<UserDto>> next, CancellationToken cancellationToken)
    {
        // validate before
        var result = await next();
        // do something after
        return result;
    }
}
```

Register it in `Program.cs`:

```csharp
services.AddCommandBehavior<ValidationBehavior>();
services.AddQueryBehavior<MyQueryBehavior>();
```
