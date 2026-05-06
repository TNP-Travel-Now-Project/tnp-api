namespace AuthApi.Application.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T? Value { get; }
        public string Error { get; }
        public string? Message { get; }

        protected Result(T? value, bool isSuccess, string error, string? message)
        {
            if (isSuccess && !string.IsNullOrEmpty(error))
                throw new ArgumentException("Success result cannot have error");

            if (!isSuccess && string.IsNullOrEmpty(error))
                throw new ArgumentException("Failure result must have error");

            Value = value;
            IsSuccess = isSuccess;
            Error = error;
            Message = message;
        }

        public static Result<T> Success(T value, string? message = null) => new Result<T>(value, true, string.Empty, message);

        public static Result<T> Fail(string error) => new Result<T>(default, false, error, string.Empty);
    }
}