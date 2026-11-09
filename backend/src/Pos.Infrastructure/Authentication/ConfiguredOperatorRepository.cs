using Microsoft.Extensions.Configuration;
using Pos.Application.Abstractions;
using Pos.Application.Models;
using System.Text.Json;

namespace Pos.Infrastructure.Authentication;

public sealed class ConfiguredOperatorRepository : IOperatorRepository
{
    private readonly List<OperatorAccount> _operators = [];

    public ConfiguredOperatorRepository(IConfiguration configuration)
    {
        var section = configuration.GetSection("Auth:Operators");
        foreach (var child in section.GetChildren())
        {
            var username = child["Username"] ?? child.Key;
            var displayName = child["DisplayName"] ?? username;
            var role = child["Role"] ?? "Cashier";
            var hash = child["PasswordHash"] ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(hash))
            {
                _operators.Add(new OperatorAccount(username, displayName, role, hash));
            }
        }
    }

    public Task<OperatorAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var normalized = username.Trim();
        var op = _operators.FirstOrDefault(o =>
            string.Equals(o.Username, normalized, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(op);
    }
}
