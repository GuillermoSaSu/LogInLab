using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogInLab.Application.DTOs
{
    public class LoginResult
    {
        public bool Success { get; private set; }
        public bool RequiresMfa { get; private set; }
        public string? ErrorMessage { get; private set; }
        public Guid? UserId { get; private set; }
        public Guid? SessionId { get; private set; }

        private LoginResult(bool success, bool requiresMfa, string? errorMessage, Guid? userId, Guid? sessionId)
        {
            Success = success;
            RequiresMfa = requiresMfa;
            ErrorMessage = errorMessage;
            UserId = userId;
            SessionId = sessionId;
        }   

        public static LoginResult SuccessResult(Guid userId, Guid sessionId)
        {
            return new LoginResult(true, false, null, userId, sessionId);
        }

        public static LoginResult MfaRequired(Guid userId)
        {
            return new LoginResult (false, true, null, userId, null);
        }

        public static LoginResult FailureResult(string errorMessage)
        {
            return new LoginResult(false, false, errorMessage, null, null);
        }   
    }
}
