using System;

namespace ApplicationScoring
{
    public static class ResultExtensions
    {
        public static Result<U, E> Map<T, U, E>(this Result<T, E> result, Func<T, U> map)
        {
            return result.Match(
                onSuccess: v => (Result<U, E>)new Success<U, E>(map(v)),
                onFailure: e => (Result<U, E>)new Failure<U, E>(e)
            );
        }

       
        public static Result<U, E> Bind<T, U, E>(this Result<T, E> result, Func<T, Result<U, E>> bind)
        {
            return result.Match(
                onSuccess: v => bind(v),
                onFailure: e => (Result<U, E>)new Failure<U, E>(e)
            );
        }
    }
}