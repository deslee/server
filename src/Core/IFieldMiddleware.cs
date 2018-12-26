using System.Threading.Tasks;
using GraphQL.Instrumentation;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server
{
    public interface IFieldMiddleware
    {
        Task<object> Resolve(ResolveFieldContext context, FieldMiddlewareDelegate next);
    }
}
