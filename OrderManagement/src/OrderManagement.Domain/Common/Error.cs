using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Common
{
    // record struct — immutable, so sánh theo giá trị
    public record Error(string Code, string Description)
    {
        // Sentinel value cho trường hợp không có lỗi
        public static readonly Error None = new(string.Empty, string.Empty);

        // Tiện ích: tạo nhanh không cần gọi new
        public static Error Create(string code, string description)
            => new(code, description);
    }

}
