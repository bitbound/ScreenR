using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Helpers
{
    public static class Time
    {
        private static TimeSpan? _offset;
        private static DateTimeOffset? _time;

        public static DateTimeOffset Now
        {
            get
            {
                var baseTime = _time ?? DateTimeOffset.Now;
                if (_offset.HasValue)
                {
                    return baseTime.Add(_offset.Value);
                }
                return baseTime;
            }
        }

        public static DateTimeOffset UtcNow => _time ?? DateTimeOffset.UtcNow;

        public static DateTimeOffset Offset(TimeSpan offset)
        {
            if (_offset.HasValue)
            {
                _offset = _offset.Value.Add(offset);
            }
            else
            {
                _offset = offset;
            }

            return Now;
        }

        public static void Restore()
        {
            _offset = null;
            _time = null;
        }

        public static void Set(DateTimeOffset time)
        {
            _offset = null;
            _time = time;
        }
    }
}
