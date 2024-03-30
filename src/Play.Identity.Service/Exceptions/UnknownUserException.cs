using System;

namespace Play.Identity.Service.Consumers;

[Serializable]
internal class UnknownUserException : Exception
{
    public UnknownUserException(Guid userId) : base($"Unknwon user '{userId}'")
    {
        this.UserId = userId;
    }

    public Guid UserId { get; }    
}
