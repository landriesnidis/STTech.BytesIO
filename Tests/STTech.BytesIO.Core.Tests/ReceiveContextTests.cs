using STTech.BytesIO.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace STTech.BytesIO.Core.Tests
{
    public class ReceiveContextTests
    {
        /// <summary>
        /// 创建一个用于测试的 ReceiveContext（使用公共构造函数，非池化模式）
        /// </summary>
        private static ReceiveContext Create(params byte[] data) => new ReceiveContext(data);

        #region 索引器测试

        [Fact]
        public void Indexer_ReturnsCorrectByte()
        {
            var ctx = Create(0x01, 0x02, 0x03, 0x04, 0x05);

            Assert.Equal(0x01, ctx[0]);
            Assert.Equal(0x03, ctx[2]);
            Assert.Equal(0x05, ctx[4]);
        }

        [Fact]
        public void Indexer_NegativeIndex_ThrowsIndexOutOfRange()
        {
            var ctx = Create(0x01, 0x02);

            Assert.Throws<IndexOutOfRangeException>(() => ctx[-1]);
        }

        [Fact]
        public void Indexer_IndexEqualToLength_ThrowsIndexOutOfRange()
        {
            var ctx = Create(0x01, 0x02, 0x03);

            Assert.Throws<IndexOutOfRangeException>(() => ctx[3]);
        }

        [Fact]
        public void Indexer_IndexBeyondLength_ThrowsIndexOutOfRange()
        {
            var ctx = Create(0x01);

            Assert.Throws<IndexOutOfRangeException>(() => ctx[5]);
        }

        [Fact]
        public void Indexer_AfterDispose_ThrowsObjectDisposed()
        {
            var ctx = Create(0x01, 0x02);
            ctx.Dispose();

            Assert.Throws<ObjectDisposedException>(() => ctx[0]);
        }

        #endregion

        #region IReadOnlyList<byte> / Count 测试

        [Fact]
        public void Count_EqualsLength()
        {
            var ctx = Create(0x01, 0x02, 0x03);
            IReadOnlyCollection<byte> collection = ctx;

            Assert.Equal(3, collection.Count);
            Assert.Equal(ctx.Length, collection.Count);
        }

        [Fact]
        public void IReadOnlyList_Indexer_WorksCorrectly()
        {
            var ctx = Create(0xAA, 0xBB, 0xCC);
            IReadOnlyList<byte> list = ctx;

            Assert.Equal(0xAA, list[0]);
            Assert.Equal(0xBB, list[1]);
            Assert.Equal(0xCC, list[2]);
        }

        #endregion

        #region 枚举器 / LINQ 测试

        [Fact]
        public void Enumerator_IteratesAllValidBytes()
        {
            byte[] expected = { 0x10, 0x20, 0x30, 0x40 };
            var ctx = Create(expected);

            var result = new List<byte>();
            foreach (var b in ctx)
            {
                result.Add(b);
            }

            Assert.Equal(expected, result.ToArray());
        }

        [Fact]
        public void Linq_SequenceEqual_Works()
        {
            byte[] data = { 0x01, 0x02, 0x03 };
            var ctx = Create(data);

            Assert.True(ctx.SequenceEqual(data));
            Assert.False(ctx.SequenceEqual(new byte[] { 0x01, 0x02, 0x04 }));
        }

        [Fact]
        public void Linq_Take_Works()
        {
            var ctx = Create(0x0A, 0x0B, 0x0C, 0x0D, 0x0E);

            var first3 = ctx.Take(3).ToArray();

            Assert.Equal(new byte[] { 0x0A, 0x0B, 0x0C }, first3);
        }

        [Fact]
        public void Linq_Skip_Works()
        {
            var ctx = Create(0x0A, 0x0B, 0x0C, 0x0D, 0x0E);

            var last2 = ctx.Skip(3).ToArray();

            Assert.Equal(new byte[] { 0x0D, 0x0E }, last2);
        }

        [Fact]
        public void Linq_Where_Works()
        {
            var ctx = Create(0x01, 0x02, 0x03, 0x04, 0x05, 0x06);

            var evens = ctx.Where(b => b % 2 == 0).ToArray();

            Assert.Equal(new byte[] { 0x02, 0x04, 0x06 }, evens);
        }

        [Fact]
        public void Linq_Select_Works()
        {
            var ctx = Create(1, 2, 3);

            var doubled = ctx.Select(b => (int)b * 2).ToArray();

            Assert.Equal(new int[] { 2, 4, 6 }, doubled);
        }

        [Fact]
        public void Linq_Count_Works()
        {
            var ctx = Create(0x01, 0x02, 0x03, 0x04, 0x05);

            Assert.Equal(5, ctx.Count());
            Assert.Equal(2, ctx.Count(b => b > 3));
        }

        [Fact]
        public void Linq_First_Last_Works()
        {
            var ctx = Create(0xAA, 0xBB, 0xCC);

            Assert.Equal(0xAA, ctx.First());
            Assert.Equal(0xCC, ctx.Last());
        }

        [Fact]
        public void Linq_Any_All_Works()
        {
            var ctx = Create(0x01, 0x02, 0x03);

            Assert.Contains((byte)0x02, ctx);
            Assert.DoesNotContain((byte)0xFF, ctx);
            Assert.True(ctx.All(b => b > 0x00));
            Assert.False(ctx.All(b => b > 0x01));
        }

        [Fact]
        public void Enumerator_AfterDispose_ThrowsObjectDisposed()
        {
            var ctx = Create(0x01, 0x02);
            ctx.Dispose();

            // yield return 使检查延迟到 MoveNext() 时执行，
            // 因此需要通过实际枚举（如 ToArray()）来触发异常
            Assert.Throws<ObjectDisposedException>(() => ctx.ToArray());
        }

        [Fact]
        public void Linq_EmptyData_Works()
        {
            var ctx = Create(Array.Empty<byte>());

            Assert.Empty(ctx);
            Assert.False(ctx.Any());
        }

        #endregion

        #region BitConverter 风格转换方法测试

        [Fact]
        public void ToBoolean_ReturnsCorrectValue()
        {
            var ctx = Create(0x00, 0x01, 0x00);

            Assert.False(ctx.ToBoolean(0));
            Assert.True(ctx.ToBoolean(1));
            Assert.False(ctx.ToBoolean(2));
        }

        [Fact]
        public void ToInt16_ReturnsCorrectValue()
        {
            short expected = 0x0102;
            byte[] bytes = BitConverter.GetBytes(expected);
            var ctx = Create(bytes);

            Assert.Equal(expected, ctx.ToInt16(0));
        }

        [Fact]
        public void ToUInt16_ReturnsCorrectValue()
        {
            ushort expected = 0xABCD;
            byte[] bytes = BitConverter.GetBytes(expected);
            var ctx = Create(bytes);

            Assert.Equal(expected, ctx.ToUInt16(0));
        }

        [Fact]
        public void ToInt32_ReturnsCorrectValue()
        {
            int expected = 123456789;
            byte[] bytes = BitConverter.GetBytes(expected);
            var ctx = Create(bytes);

            Assert.Equal(expected, ctx.ToInt32(0));
        }

        [Fact]
        public void ToUInt32_ReturnsCorrectValue()
        {
            uint expected = 0xDEADBEEF;
            byte[] bytes = BitConverter.GetBytes(expected);
            var ctx = Create(bytes);

            Assert.Equal(expected, ctx.ToUInt32(0));
        }

        [Fact]
        public void ToInt64_ReturnsCorrectValue()
        {
            long expected = 1234567890123456789L;
            byte[] bytes = BitConverter.GetBytes(expected);
            var ctx = Create(bytes);

            Assert.Equal(expected, ctx.ToInt64(0));
        }

        [Fact]
        public void ToUInt64_ReturnsCorrectValue()
        {
            ulong expected = 0xDEADBEEFCAFEBABE;
            byte[] bytes = BitConverter.GetBytes(expected);
            var ctx = Create(bytes);

            Assert.Equal(expected, ctx.ToUInt64(0));
        }

        [Fact]
        public void ToSingle_ReturnsCorrectValue()
        {
            float expected = 3.14f;
            byte[] bytes = BitConverter.GetBytes(expected);
            var ctx = Create(bytes);

            Assert.Equal(expected, ctx.ToSingle(0));
        }

        [Fact]
        public void ToDouble_ReturnsCorrectValue()
        {
            double expected = 3.141592653589793;
            byte[] bytes = BitConverter.GetBytes(expected);
            var ctx = Create(bytes);

            Assert.Equal(expected, ctx.ToDouble(0));
        }

        [Fact]
        public void ToChar_ReturnsCorrectValue()
        {
            char expected = 'A';
            byte[] bytes = BitConverter.GetBytes(expected);
            var ctx = Create(bytes);

            Assert.Equal(expected, ctx.ToChar(0));
        }

        [Fact]
        public void BitConverter_WithOffset_ReturnsCorrectValue()
        {
            // 构建 [0xAA, int32值(4字节), 0xBB] 的数据
            int expected = 42;
            byte[] intBytes = BitConverter.GetBytes(expected);
            byte[] data = new byte[1 + intBytes.Length + 1];
            data[0] = 0xAA;
            Buffer.BlockCopy(intBytes, 0, data, 1, intBytes.Length);
            data[data.Length - 1] = 0xBB;

            var ctx = Create(data);

            Assert.Equal(0xAA, ctx[0]);
            Assert.Equal(expected, ctx.ToInt32(1));
            Assert.Equal(0xBB, ctx[5]);
        }

        [Fact]
        public void BitConverter_MultipleValuesInSequence()
        {
            // 构建 [short, int, double] 连续数据
            short s = 1234;
            int i = 567890;
            double d = 1.23456;

            byte[] sBytes = BitConverter.GetBytes(s);
            byte[] iBytes = BitConverter.GetBytes(i);
            byte[] dBytes = BitConverter.GetBytes(d);

            byte[] data = new byte[sBytes.Length + iBytes.Length + dBytes.Length];
            Buffer.BlockCopy(sBytes, 0, data, 0, sBytes.Length);
            Buffer.BlockCopy(iBytes, 0, data, sBytes.Length, iBytes.Length);
            Buffer.BlockCopy(dBytes, 0, data, sBytes.Length + iBytes.Length, dBytes.Length);

            var ctx = Create(data);

            Assert.Equal(s, ctx.ToInt16(0));
            Assert.Equal(i, ctx.ToInt32(2));
            Assert.Equal(d, ctx.ToDouble(6));
        }

        [Fact]
        public void BitConverter_OutOfRange_ThrowsArgumentOutOfRange()
        {
            var ctx = Create(0x01, 0x02);

            // ToInt32 需要 4 字节，但只有 2 字节
            Assert.Throws<ArgumentOutOfRangeException>(() => ctx.ToInt32(0));
        }

        [Fact]
        public void BitConverter_NegativeIndex_ThrowsArgumentOutOfRange()
        {
            var ctx = Create(0x01, 0x02, 0x03, 0x04);

            Assert.Throws<ArgumentOutOfRangeException>(() => ctx.ToInt32(-1));
        }

        [Fact]
        public void BitConverter_AfterDispose_ThrowsObjectDisposed()
        {
            var ctx = Create(0x01, 0x02, 0x03, 0x04);
            ctx.Dispose();

            Assert.Throws<ObjectDisposedException>(() => ctx.ToInt32(0));
        }

        #endregion

        #region 大端序 (BigEndian) 转换测试

        [Fact]
        public void ToInt16_BigEndian_ReturnsCorrectValue()
        {
            // 大端序 0x0102 = 258
            var ctx = Create(0x01, 0x02);

            Assert.Equal(0x0102, ctx.ToInt16(0, bigEndian: true));
        }

        [Fact]
        public void ToUInt16_BigEndian_ReturnsCorrectValue()
        {
            // 大端序 0xABCD
            var ctx = Create(0xAB, 0xCD);

            Assert.Equal((ushort)0xABCD, ctx.ToUInt16(0, bigEndian: true));
        }

        [Fact]
        public void ToInt32_BigEndian_ReturnsCorrectValue()
        {
            // 大端序 0x00000001 = 1
            var ctx = Create(0x00, 0x00, 0x00, 0x01);

            Assert.Equal(1, ctx.ToInt32(0, bigEndian: true));
        }

        [Fact]
        public void ToUInt32_BigEndian_ReturnsCorrectValue()
        {
            // 大端序 0xDEADBEEF
            var ctx = Create(0xDE, 0xAD, 0xBE, 0xEF);

            Assert.Equal(0xDEADBEEFu, ctx.ToUInt32(0, bigEndian: true));
        }

        [Fact]
        public void ToInt64_BigEndian_ReturnsCorrectValue()
        {
            // 大端序 0x0000000000000001 = 1
            var ctx = Create(0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01);

            Assert.Equal(1L, ctx.ToInt64(0, bigEndian: true));
        }

        [Fact]
        public void ToUInt64_BigEndian_ReturnsCorrectValue()
        {
            // 大端序 0xDEADBEEFCAFEBABE
            var ctx = Create(0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE, 0xBA, 0xBE);

            Assert.Equal(0xDEADBEEFCAFEBABEuL, ctx.ToUInt64(0, bigEndian: true));
        }

        [Fact]
        public void ToSingle_BigEndian_ReturnsCorrectValue()
        {
            // 将 float 转为小端字节，反转后作为大端输入
            float expected = 3.14f;
            byte[] leBytes = BitConverter.GetBytes(expected);
            byte[] beBytes = leBytes.Reverse().ToArray();

            var ctx = Create(beBytes);

            Assert.Equal(expected, ctx.ToSingle(0, bigEndian: true));
        }

        [Fact]
        public void ToDouble_BigEndian_ReturnsCorrectValue()
        {
            double expected = 3.141592653589793;
            byte[] leBytes = BitConverter.GetBytes(expected);
            byte[] beBytes = leBytes.Reverse().ToArray();

            var ctx = Create(beBytes);

            Assert.Equal(expected, ctx.ToDouble(0, bigEndian: true));
        }

        [Fact]
        public void ToInt16_BigEndian_WithOffset()
        {
            // [0xFF, 大端序 0x0102, 0xFF]
            var ctx = Create(0xFF, 0x01, 0x02, 0xFF);

            Assert.Equal(0x0102, ctx.ToInt16(1, bigEndian: true));
        }

        [Fact]
        public void ToInt32_BigEndian_MatchesManualReverse()
        {
            // 验证 bigEndian 与手动 Reverse 一致（模拟 Modbus 场景）
            int expected = 12345;
            byte[] leBytes = BitConverter.GetBytes(expected);
            byte[] beBytes = leBytes.Reverse().ToArray();

            var ctx = Create(beBytes);

            // 大端序读取
            int bigEndianResult = ctx.ToInt32(0, bigEndian: true);

            // 手动 Reverse 后小端序读取（旧方式）
            int manualResult = BitConverter.ToInt32(beBytes.Reverse().ToArray(), 0);

            Assert.Equal(expected, bigEndianResult);
            Assert.Equal(manualResult, bigEndianResult);
        }

        [Fact]
        public void DefaultEndianness_IsBackwardCompatible()
        {
            // 不传 bigEndian 参数时行为与之前一致（系统默认字节序）
            int expected = 42;
            byte[] bytes = BitConverter.GetBytes(expected);
            var ctx = Create(bytes);

            Assert.Equal(expected, ctx.ToInt32(0));
            Assert.Equal(expected, ctx.ToInt32(0, bigEndian: false));
        }

        #endregion

        #region ToHexString 测试

        [Fact]
        public void ToHexString_ReturnsCorrectFormat()
        {
            var ctx = Create(0x0A, 0x1B, 0x2C);

            Assert.Equal("0A-1B-2C", ctx.ToHexString());
        }

        [Fact]
        public void ToHexString_WithRange_ReturnsSubset()
        {
            var ctx = Create(0x0A, 0x1B, 0x2C, 0x3D, 0x4E);

            Assert.Equal("1B-2C-3D", ctx.ToHexString(1, 3));
        }

        [Fact]
        public void ToHexString_EmptyData_ReturnsEmpty()
        {
            var ctx = Create(Array.Empty<byte>());

            Assert.Equal(string.Empty, ctx.ToHexString());
        }

        [Fact]
        public void ToHexString_SingleByte()
        {
            var ctx = Create(0xFF);

            Assert.Equal("FF", ctx.ToHexString());
        }

        #endregion

        #region CopyTo 测试

        [Fact]
        public void CopyTo_CopiesCorrectData()
        {
            var ctx = Create(0x01, 0x02, 0x03);
            byte[] dest = new byte[5];

            ctx.CopyTo(dest, 1);

            Assert.Equal(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x00 }, dest);
        }

        [Fact]
        public void CopyTo_NullDestination_ThrowsArgumentNull()
        {
            var ctx = Create(0x01);

            Assert.Throws<ArgumentNullException>(() => ctx.CopyTo(null!, 0));
        }

        [Fact]
        public void CopyTo_InsufficientSpace_ThrowsArgumentOutOfRange()
        {
            var ctx = Create(0x01, 0x02, 0x03);
            byte[] dest = new byte[2];

            Assert.Throws<ArgumentOutOfRangeException>(() => ctx.CopyTo(dest, 0));
        }

        #endregion

        #region 边界场景

        [Fact]
        public void EmptyData_LengthIsZero()
        {
            var ctx = Create(Array.Empty<byte>());

            Assert.Equal(0, ctx.Length);
            Assert.Equal(string.Empty, ctx.ToHexString());
        }

        [Fact]
        public void ToArray_StillWorks()
        {
            byte[] data = { 0x01, 0x02, 0x03 };
            var ctx = Create(data);

            Assert.Equal(data, ctx.ToArray());
        }

        [Fact]
        public void EncodeToString_ReturnsCorrectValue()
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello");
            var ctx = Create(data);

            Assert.Equal("Hello", ctx.EncodeToString(System.Text.Encoding.UTF8));
        }

        #endregion

        #region 池化与生命周期测试

        [Fact]
        public void PooledConstructor_StoresRentedArrayAndOffset()
        {
            byte[] rented = System.Buffers.ArrayPool<byte>.Shared.Rent(100);
            rented[10] = 0xAA;
            rented[11] = 0xBB;

            // 调用内部构造函数
            var ctx = new ReceiveContext(rented, 10, 2);

            Assert.Equal(2, ctx.Length);
            Assert.Equal(0xAA, ctx[0]);
            Assert.Equal(0xBB, ctx[1]);
            Assert.False(ctx.IsDisposed);

            ctx.Dispose();
            Assert.True(ctx.IsDisposed);
        }

        [Fact]
        public void ReferenceCounting_IncrRefAndDispose()
        {
            byte[] rented = System.Buffers.ArrayPool<byte>.Shared.Rent(50);
            var ctx = new ReceiveContext(rented, 0, 10);

            // 初始引用计数为 1
            Assert.False(ctx.IsDisposed);

            // 增加引用计数 -> 2
            ctx.IncrRef();
            
            // 第一次 Dispose -> 计数减为 1，不应该真正释放
            ctx.Dispose();
            Assert.False(ctx.IsDisposed);

            // 第二次 Dispose -> 计数减为 0，应该释放
            ctx.Dispose();
            Assert.True(ctx.IsDisposed);
        }

        [Fact]
        public void DoubleDispose_IsSafe()
        {
            byte[] rented = System.Buffers.ArrayPool<byte>.Shared.Rent(50);
            var ctx = new ReceiveContext(rented, 0, 10);

            ctx.Dispose();
            Assert.True(ctx.IsDisposed);

            // 再次调用 Dispose 不应抛出异常
            var ex = Record.Exception(() => ctx.Dispose());
            Assert.Null(ex);
        }

        [Fact]
        public void AccessAfterDispose_ThrowsObjectDisposedException()
        {
            byte[] rented = System.Buffers.ArrayPool<byte>.Shared.Rent(50);
            var ctx = new ReceiveContext(rented, 0, 10);
            ctx.Dispose();

            Assert.Throws<ObjectDisposedException>(() => ctx[0]);
            Assert.Throws<ObjectDisposedException>(() => ctx.Memory);
            Assert.Throws<ObjectDisposedException>(() => ctx.ToArray());
            Assert.Throws<ObjectDisposedException>(() => ctx.ToHexString());
            Assert.Throws<ObjectDisposedException>(() => ctx.CopyTo(new byte[10], 0));
        }

        #endregion
    }
}
