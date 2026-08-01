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
        public string? ErrorMessage { get; private set; }
        public Guid? UserId { get; private set; }
        public Guid? SessionId { get; private set; }

        private LoginResult(bool success, string? errorMessage, Guid? userId, Guid? sessionId)
        {
            Success = success;
            ErrorMessage = errorMessage;
            UserId = userId;
            SessionId = sessionId;
        }   

        public static LoginResult SuccessResult(Guid userId, Guid sessionId)
        {
            return new LoginResult(true, null, userId, sessionId);
        }

        public static LoginResult FailureResult(string errorMessage)
        {
            return new LoginResult(false, errorMessage, null, null);
        }   
    }
}
