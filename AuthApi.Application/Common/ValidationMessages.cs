namespace AuthApi.Application.Common
{
    public static class ValidationMessages
    {
        public const string FieldRequired = "{PropertyName} không được để trống";
        public const string EmailInvalid = "Email không đúng định dạng";
        public const string EmailMaxLength = "Email không được vượt quá {MaxLength} ký tự";

        public const string PasswordMinLength = "Mật khẩu phải có ít nhất {MinLength} ký tự";
        public const string PasswordMaxLength = "Mật khẩu không được vượt quá {MaxLength} ký tự";
        public const string PasswordLengthRange = "Mật khẩu phải có độ dài từ {MinLength} đến {MaxLength} ký tự";
        public const string PasswordRequiresUppercase = "Mật khẩu phải chứa ít nhất 1 chữ hoa";
        public const string PasswordRequiresLowercase = "Mật khẩu phải chứa ít nhất 1 chữ thường";
        public const string PasswordRequiresDigit = "Mật khẩu phải chứa ít nhất 1 chữ số";
        public const string PasswordRequiresSpecial = "Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt (~!@#$%^&*()_+=?)";
        public const string PasswordsMustMatch = "Mật khẩu xác nhận phải trùng với mật khẩu";

        public const string OtpRequired = "Mã OTP không được để trống";
        public const string OtpLength = "Mã OTP phải có đúng {ExpectedLength} chữ số";
        public const string OtpDigits = "Mã OTP chỉ được chứa chữ số";

        public const string FullNameRequired = "Họ và tên không được để trống";
        public const string FullNameMaxLength = "Họ và tên không được vượt quá {MaxLength} ký tự";

        public const string UserNameRequired = "Tên đăng nhập không được để trống";
        public const string UserNameMaxLength = "Tên đăng nhập không được vượt quá {MaxLength} ký tự";

        public const string PhoneNumberRequired = "Số điện thoại không được để trống";
        public const string PhoneNumberLength = "Số điện thoại phải có đúng {ExpectedLength} chữ số";
        public const string PhoneNumberDigits = "Số điện thoại chỉ được chứa chữ số (0-9)";

        public const string DateOfBirthRequired = "Ngày sinh không được để trống";
        public const string DateOfBirthRange = "Bạn phải từ {From} đến {To} tuổi";

        public const string ValidationFailed = "Dữ liệu không hợp lệ";
        public const string InternalServerError = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau";
    }
}
