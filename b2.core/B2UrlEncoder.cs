﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.wibblr.b2
{
    /// <summary>
    /// A Url encoder that passes the vendor supplied test cases (see https://www.backblaze.com/b2/docs/string_encoding.html)
    /// 
    /// The library functions Uri.EscapeUriString or Uri.EscapeDataString both fail these tests.
    /// </summary>
    public class B2UrlEncoder
    {
        static HashSet<byte> literals = new HashSet<byte>("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._-/~!$'()*;=:@".Select(c => (byte)c));

        internal static char EncodeHexDigit(int b)
        {
            if (b > 15 || b < 0)
                throw new ArgumentException($"Cannot convert integer {b} to hex digit");

            return (char)((b >= 10) ? (b - 10 + 'A') : (b + '0'));
        }

        internal static int DecodeHexDigit(char c)
        {
            if (c >= '0' && c <= '9')
                return c - '0';
            else if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;

            throw new ArgumentException($"Unable to parse '{c}' as a hex digit");
        }

        public static string Encode(string s)
        {
            var sb = new StringBuilder();

            foreach (var b in Encoding.UTF8.GetBytes(s))
            {
                if (literals.Contains(b))
                {
                    sb.Append((char)b);
                }
                else
                {
                    sb.Append('%');
                    sb.Append(EncodeHexDigit(b / 16));
                    sb.Append(EncodeHexDigit(b % 16));
                }
            }

            return sb.ToString();
        }

        public static string Decode(string s)
        {
            var len = s.Length;

            // Allocate enough space to hold the decoded bytes
            // This will always be equal or less than the length the encoded string, so 
            // use that.
            var bytes = new byte[len];
            var pos = 0;

            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c > 0xFF)
                    throw new ArgumentException($"Invalid URL encoded string '{s}': found 16-bit code point at position {i}");

                var b = (byte)c;
               
                if (literals.Contains(b))
                    bytes[pos++] = b;
                else if (c == '+') // special case
                    bytes[pos++] = (byte)' ';
                else
                {
                    if (c != '%')
                        throw new ArgumentException($"Invalid URL encoded string '{s}' - invalid character '{c}' at position {i}");
                    if ((i + 2) >= len)
                        throw new ArgumentException($"Invalid URL encoded string '{s}' - Expected hex digit but string was truncated");
                    try
                    {
                        var upperNybble = DecodeHexDigit(s[++i]);
                        var lowerNybble = DecodeHexDigit(s[++i]);
                        bytes[pos++] = (byte)((upperNybble * 16) + lowerNybble);
                    }
                    catch (ArgumentException ae)
                    {
                        throw new ArgumentException($"Invalid URL encoded string '{s}' at position {i} - {ae.Message}");
                    }
                }
            }
            return Encoding.UTF8.GetString(bytes, 0, pos);
        }
    }
}
