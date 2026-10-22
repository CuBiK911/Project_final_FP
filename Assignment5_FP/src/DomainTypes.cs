using System;

namespace ApplicationScoring
{
    
    public abstract record Result<T, E>
    {
        public abstract TResult Match<TResult>(Func<T, TResult> onSuccess, Func<E, TResult> onFailure);
    }

    public sealed record Success<T, E>(T Value) : Result<T, E>
    {
        public override TResult Match<TResult>(Func<T, TResult> onSuccess, Func<E, TResult> onFailure)
            => onSuccess(Value);
    }

    public sealed record Failure<T, E>(E Error) : Result<T, E>
    {
        public override TResult Match<TResult>(Func<T, TResult> onSuccess, Func<E, TResult> onFailure)
            => onFailure(Error);
    }

   
    public abstract record Maybe<T>
    {
        public abstract TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone);
    }

    public sealed record Some<T>(T Value) : Maybe<T>
    {
        public override TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone)
            => onSome(Value);
    }

    public sealed record None<T>() : Maybe<T>
    {
        public override TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone)
            => onNone();
    }

    
    public enum ApplicationError
    {
        InvalidGpa,
        MissingApplicant,
        StateTransitionNotAllowed,
        FileNotFound,
        MalformedFile
    }

    public record DomainError(ApplicationError Code, string Message);
}