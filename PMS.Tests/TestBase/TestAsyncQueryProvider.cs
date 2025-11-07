using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;

namespace PMS.Tests.TestBase
{
    // ====================== PROVIDER ======================
    public class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

        public IQueryable CreateQuery(Expression expression)
            => (IQueryable)Activator.CreateInstance(
                typeof(TestAsyncEnumerable<>).MakeGenericType(expression.Type.GenericTypeArguments[0]),
                expression)!;

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new TestAsyncEnumerable<TElement>(expression);

        public object Execute(Expression expression) => _inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

        // async – chỉ chuyển sang đồng bộ vì dữ liệu đã có trong bộ nhớ
        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken ct = default)
            => Task.FromResult(_inner.Execute<TResult>(expression));

        TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken ct)
            => _inner.Execute<TResult>(expression);
    }

    // ====================== ENUMERABLE ======================
    public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        private readonly IQueryProvider _provider;

        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        {
            _provider = new TestAsyncQueryProvider<T>(enumerable.AsQueryable().Provider);
        }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        {
            // Khi tạo từ expression, dùng LINQ provider mặc định
            _provider = new TestAsyncQueryProvider<T>(((IQueryable<T>)new EnumerableQuery<T>(expression)).Provider);

        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct = default)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IQueryProvider IQueryable.Provider => _provider;
    }


    // ====================== ENUMERATOR ======================
    public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
            => new ValueTask<bool>(_inner.MoveNext());
    }

    // ====================== EXTENSION ======================
    public static class AsyncQueryableExtensions
    {
        public static IQueryable<T> ToAsyncQueryable<T>(this IEnumerable<T> source)
            => new TestAsyncEnumerable<T>(source);
    }
}
