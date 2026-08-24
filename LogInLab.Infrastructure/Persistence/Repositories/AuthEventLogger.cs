using LogInLab.Application.Interfaces;
using LogInLab.Domain.Entities;
using LogInLab.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogInLab.Infrastructure.Persistence.Repositories
{
    public class AuthEventLogger : IAuthEventLogger
    {
        private readonly LogInLabDbContext _context;

        public AuthEventLogger(LogInLabDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(AuthEventType authEventType, string ipAddress, string userAgent, Guid? userId = null, string? email = null)
        {
            AuthEvent authEvent = new AuthEvent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventType = authEventType,
                Email = email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
            };

            await _context.AuthEvents.AddAsync(authEvent);
            await _context.SaveChangesAsync();

        }
    }
}
