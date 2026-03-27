using System.Collections.ObjectModel;
using System.Text;
using SECS2GEM.Core.Enums;

namespace SECS2GEM.Core.Entities
{
    /// <summary>
    /// SECS-II数据项
    /// </summary>
    /// <remarks>
    /// 设计思路：
    /// 1. 使用不可变设计，确保线程安全
    /// 2. 支持递归结构（List可包含子项）
    /// 3. 提供类型安全的值访问方法
    /// 4. 通过静态工厂方法创建实例，隐藏构造复杂性
    /// 
    /// 数据项结构：
    /// ┌────────────┐┌────────────────────┐┌────────────────────┐
    /// │ Format Byte ││   Length Bytes     ││     Data Bytes     │
    /// │   (1字节)   ││   (1-3字节)        ││   (可变长度)       │
    /// └────────────┘└────────────────────┘└────────────────────┘
    /// </remarks>
    public sealed class SecsItem
    {
        private readonly object _value;
        private readonly ReadOnlyCollection<SecsItem>? _items;

        /// <summary>
        /// 数据格式
        /// </summary>
        public SecsFormat Format { get; }

        /// <summary>
        /// 数据项数量（List类型为子项数量，其他类型为元素数量）
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// 子项集合（仅List类型有效）
        /// </summary>
        public ReadOnlyCollection<SecsItem> Items =>
            _items ?? throw new InvalidOperationException($"Format {Format} does not support Items property.");

        /// <summary>
        /// 原始值
        /// </summary>
        public object Value => _value;

        #region Private Constructor

        private SecsItem(SecsFormat format, object value, int count)
        {
            Format = format;
            _value = value;
            Count = count;
            _items = null;
        }

        private SecsItem(SecsFormat format, IList<SecsItem> items)
        {
            Format = format;
            _value = items;
            Count = items.Count;
            _items = new ReadOnlyCollection<SecsItem>(items.ToList());
        }

        #endregion

        #region Static Factory Methods - List

        /// <summary>
        /// 创建列表数据项
        /// </summary>
        /// <param name="items">子项数组</param>
        /// <returns>List类型的SecsItem</returns>
        public static SecsItem L(params SecsItem[] items)
        {
            return new SecsItem(SecsFormat.List, items);
        }

        /// <summary>
        /// 创建列表数据项
        /// </summary>
        /// <param name="items">子项集合</param>
        /// <returns>List类型的SecsItem</returns>
        public static SecsItem L(IEnumerable<SecsItem> items)
        {
            return new SecsItem(SecsFormat.List, items.ToList());
        }

        #endregion

        #region Static Factory Methods - String

        /// <summary>
        /// 创建ASCII字符串数据项
        /// </summary>
        /// <param name="value">字符串值</param>
        /// <returns>ASCII类型的SecsItem</returns>
        public static SecsItem A(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.ASCII, value, value.Length);
        }

        /// <summary>
        /// 创建JIS-8字符串数据项
        /// </summary>
        /// <param name="value">字符串值</param>
        /// <returns>JIS8类型的SecsItem</returns>
        public static SecsItem J(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.JIS8, value, value.Length);
        }

        /// <summary>
        /// 创建Unicode字符串数据项
        /// </summary>
        /// <param name="value">字符串值</param>
        /// <returns>Unicode类型的SecsItem</returns>
        public static SecsItem U(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.Unicode, value, value.Length);
        }

        #endregion

        #region Static Factory Methods - Binary and Boolean

        /// <summary>
        /// 创建二进制数据项
        /// </summary>
        /// <param name="value">字节数组</param>
        /// <returns>Binary类型的SecsItem</returns>
        public static SecsItem B(params byte[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.Binary, value.ToArray(), value.Length);
        }

        /// <summary>
        /// 创建二进制数据项
        /// </summary>
        /// <param name="value">字节集合</param>
        /// <returns>Binary类型的SecsItem</returns>
        public static SecsItem B(IEnumerable<byte> value)
        {
            ArgumentNullException.ThrowIfNull(value);
            var arr = value.ToArray();
            return new SecsItem(SecsFormat.Binary, arr, arr.Length);
        }

        /// <summary>
        /// 创建布尔数据项
        /// </summary>
        /// <param name="value">布尔值数组</param>
        /// <returns>Boolean类型的SecsItem</returns>
        public static SecsItem Boolean(params bool[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.Boolean, value.ToArray(), value.Length);
        }

        #endregion

        #region Static Factory Methods - Signed Integers

        /// <summary>
        /// 创建1字节有符号整数数据项
        /// </summary>
        public static SecsItem I1(params sbyte[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.I1, value.ToArray(), value.Length);
        }

        /// <summary>
        /// 创建2字节有符号整数数据项
        /// </summary>
        public static SecsItem I2(params short[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.I2, value.ToArray(), value.Length);
        }

        /// <summary>
        /// 创建4字节有符号整数数据项
        /// </summary>
        public static SecsItem I4(params int[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.I4, value.ToArray(), value.Length);
        }

        /// <summary>
        /// 创建8字节有符号整数数据项
        /// </summary>
        public static SecsItem I8(params long[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.I8, value.ToArray(), value.Length);
        }

        #endregion

        #region Static Factory Methods - Unsigned Integers

        /// <summary>
        /// 创建1字节无符号整数数据项
        /// </summary>
        public static SecsItem U1(params byte[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.U1, value.ToArray(), value.Length);
        }

        /// <summary>
        /// 创建2字节无符号整数数据项
        /// </summary>
        public static SecsItem U2(params ushort[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.U2, value.ToArray(), value.Length);
        }

        /// <summary>
        /// 创建4字节无符号整数数据项
        /// </summary>
        public static SecsItem U4(params uint[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.U4, value.ToArray(), value.Length);
        }

        /// <summary>
        /// 创建8字节无符号整数数据项
        /// </summary>
        public static SecsItem U8(params ulong[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.U8, value.ToArray(), value.Length);
        }

        #endregion

        #region Static Factory Methods - Floating Point

        /// <summary>
        /// 创建4字节浮点数数据项
        /// </summary>
        public static SecsItem F4(params float[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.F4, value.ToArray(), value.Length);
        }

        /// <summary>
        /// 创建8字节浮点数数据项
        /// </summary>
        public static SecsItem F8(params double[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new SecsItem(SecsFormat.F8, value.ToArray(), value.Length);
        }

        #endregion

        #region Value Accessors

        /// <summary>
        /// 获取字符串值
        /// </summary>
        /// <exception cref="InvalidOperationException">当格式不是字符串类型时抛出</exception>
        public string GetString()
        {
            if (Format is SecsFormat.ASCII or SecsFormat.JIS8 or SecsFormat.Unicode)
            {
                return (string)_value;
            }
            throw new InvalidOperationException($"Cannot get string from format {Format}");
        }

        /// <summary>
        /// 获取字节数组
        /// </summary>
        /// <exception cref="InvalidOperationException">当格式不是Binary类型时抛出</exception>
        public byte[] GetBytes()
        {
            if (Format == SecsFormat.Binary)
            {
                return ((byte[])_value).ToArray();
            }
            throw new InvalidOperationException($"Cannot get bytes from format {Format}");
        }

        /// <summary>
        /// 获取布尔数组
        /// </summary>
        public bool[] GetBooleans()
        {
            if (Format == SecsFormat.Boolean)
            {
                return ((bool[])_value).ToArray();
            }
            throw new InvalidOperationException($"Cannot get booleans from format {Format}");
        }

        /// <summary>
        /// 获取第一个布尔值
        /// </summary>
        public bool GetBoolean()
        {
            var arr = GetBooleans();
            return arr.Length > 0 ? arr[0] : false;
        }

        /// <summary>
        /// 获取有符号整数数组（自动转换）
        /// </summary>
        public long[] GetInt64Array()
        {
            return Format switch
            {
                SecsFormat.I1 => ((sbyte[])_value).Select(x => (long)x).ToArray(),
                SecsFormat.I2 => ((short[])_value).Select(x => (long)x).ToArray(),
                SecsFormat.I4 => ((int[])_value).Select(x => (long)x).ToArray(),
                SecsFormat.I8 => ((long[])_value).ToArray(),
                _ => throw new InvalidOperationException($"Cannot get signed integers from format {Format}")
            };
        }

        /// <summary>
        /// 获取无符号整数数组（自动转换）
        /// </summary>
        public ulong[] GetUInt64Array()
        {
            return Format switch
            {
                SecsFormat.U1 => ((byte[])_value).Select(x => (ulong)x).ToArray(),
                SecsFormat.U2 => ((ushort[])_value).Select(x => (ulong)x).ToArray(),
                SecsFormat.U4 => ((uint[])_value).Select(x => (ulong)x).ToArray(),
                SecsFormat.U8 => ((ulong[])_value).ToArray(),
                _ => throw new InvalidOperationException($"Cannot get unsigned integers from format {Format}")
            };
        }

        /// <summary>
        /// 获取浮点数数组（自动转换为double）
        /// </summary>
        public double[] GetDoubleArray()
        {
            return Format switch
            {
                SecsFormat.F4 => ((float[])_value).Select(x => (double)x).ToArray(),
                SecsFormat.F8 => ((double[])_value).ToArray(),
                _ => throw new InvalidOperationException($"Cannot get floating point numbers from format {Format}")
            };
        }

        /// <summary>
        /// 获取第一个整数值（自动转换）
        /// </summary>
        public long GetInt64()
        {
            var arr = GetInt64Array();
            return arr.Length > 0 ? arr[0] : 0;
        }

        /// <summary>
        /// 获取第一个无符号整数值（自动转换）
        /// </summary>
        public ulong GetUInt64()
        {
            var arr = GetUInt64Array();
            return arr.Length > 0 ? arr[0] : 0;
        }

        /// <summary>
        /// 获取第一个浮点数值
        /// </summary>
        public double GetDouble()
        {
            var arr = GetDoubleArray();
            return arr.Length > 0 ? arr[0] : 0.0;
        }

        #endregion

        #region Indexer

        /// <summary>
        /// 获取指定索引的子项（仅List类型有效）
        /// </summary>
        public SecsItem this[int index]
        {
            get
            {
                if (Format != SecsFormat.List)
                {
                    throw new InvalidOperationException($"Cannot index into format {Format}");
                }
                return Items[index];
            }
        }

        #endregion

        #region Object Overrides

        /// <summary>
        /// 返回数据项的字符串表示
        /// </summary>
        public override string ToString()
        {
            return Format switch
            {
                SecsFormat.List => $"L,{Count}",
                SecsFormat.ASCII => $"A \"{GetString()}\"",
                SecsFormat.JIS8 => $"J \"{GetString()}\"",
                SecsFormat.Unicode => $"U \"{GetString()}\"",
                SecsFormat.Binary => $"B[{Count}]",
                SecsFormat.Boolean => $"Boolean {string.Join(",", GetBooleans())}",
                SecsFormat.I1 => $"I1 {FormatArray(GetInt64Array())}",
                SecsFormat.I2 => $"I2 {FormatArray(GetInt64Array())}",
                SecsFormat.I4 => $"I4 {FormatArray(GetInt64Array())}",
                SecsFormat.I8 => $"I8 {FormatArray(GetInt64Array())}",
                SecsFormat.U1 => $"U1 {FormatArray(GetUInt64Array())}",
                SecsFormat.U2 => $"U2 {FormatArray(GetUInt64Array())}",
                SecsFormat.U4 => $"U4 {FormatArray(GetUInt64Array())}",
                SecsFormat.U8 => $"U8 {FormatArray(GetUInt64Array())}",
                SecsFormat.F4 => $"F4 {FormatArray(GetDoubleArray())}",
                SecsFormat.F8 => $"F8 {FormatArray(GetDoubleArray())}",
                _ => $"Unknown({Format})"
            };
        }

        private static string FormatArray<T>(T[] arr)
        {
            if (arr.Length == 0) return "[]";
            if (arr.Length == 1) return arr[0]?.ToString() ?? "";
            return $"[{string.Join(", ", arr)}]";
        }

        /// <summary>
        /// 生成SML格式的字符串表示（含缩进）
        /// </summary>
        /// <param name="indent">缩进级别</param>
        /// <returns>SML格式字符串</returns>
        public string ToSml(int indent = 0)
        {
            var sb = new StringBuilder();
            ToSmlInternal(sb, indent);
            return sb.ToString();
        }

        private void ToSmlInternal(StringBuilder sb, int indent)
        {
            var prefix = new string(' ', indent * 2);

            if (Format == SecsFormat.List)
            {
                sb.AppendLine($"{prefix}<L [{Count}]");
                foreach (var item in Items)
                {
                    item.ToSmlInternal(sb, indent + 1);
                }
                sb.AppendLine($"{prefix}>");
            }
            else
            {
                sb.AppendLine($"{prefix}<{this}>");
            }
        }

        #endregion
    }
}
