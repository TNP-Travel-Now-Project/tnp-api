namespace AuthApi.Application.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T? Value { get; }
        public Error? Error { get; }
        public string? Message { get; }

        protected Result(T? value, bool isSuccess, Error? error, string? message)
        {
            if (isSuccess && error is not null)
                throw new ArgumentException("Success result cannot have error");

            if (!isSuccess && error is null)
                throw new ArgumentException("Failure result must have error");

            Value = value;
            IsSuccess = isSuccess;
            Error = error;
            Message = message;
        }

        public static Result<T> Success(T value, string? message = null)
            => new Result<T>(value, true, null, message);

        public static Result<T> Fail(Error error)
            => new Result<T>(default, false, error, null);

        public static Result<T> Fail(string error)
            => new Result<T>(default, false, new Error(ErrorCodes.GeneralError, error), null);
    }
}