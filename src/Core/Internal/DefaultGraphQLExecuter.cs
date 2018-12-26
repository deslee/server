using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Internal
{
    public class DefaultGraphQLExecuter<TSchema> : IGraphQLExecuter<TSchema>
        where TSchema : ISchema
    {
        public TSchema Schema { get; }

        private readonly IDocumentExecuter _documentExecuter;
        private readonly GraphQLOptions _options;
        private readonly IEnumerable<IDocumentExecutionListener> _listeners;
        private readonly IEnumerable<IValidationRule> _validationRules;
        private readonly IEnumerable<IFieldMiddleware> _fieldMiddleware;

        public DefaultGraphQLExecuter(
            TSchema schema,
            IDocumentExecuter documentExecuter,
            IOptions<GraphQLOptions> options,
            IEnumerable<IDocumentExecutionListener> listeners,
            IEnumerable<IValidationRule> validationRules,
            IEnumerable<IFieldMiddleware> fieldMiddleware
        )
        {
            Schema = schema;

            _documentExecuter = documentExecuter;
            _options = options.Value;
            _listeners = listeners;
            _validationRules = validationRules;
            _fieldMiddleware = fieldMiddleware;
        }

        public virtual Task<ExecutionResult> ExecuteAsync(string operationName, string query, Inputs variables, object context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var options = GetOptions(operationName, query, variables, context, cancellationToken);
            return _documentExecuter.ExecuteAsync(options);
        }

        protected virtual ExecutionOptions GetOptions(string operationName, string query, Inputs variables, object context, CancellationToken cancellationToken)
        {
            var opts = new ExecutionOptions()
            {
                Schema = Schema,
                OperationName = operationName,
                Query = query,
                Inputs = variables,
                UserContext = context,
                CancellationToken = cancellationToken,
                ComplexityConfiguration = _options.ComplexityConfiguration,
                EnableMetrics = _options.EnableMetrics,
                ExposeExceptions = _options.ExposeExceptions,
                SetFieldMiddleware = _options.SetFieldMiddleware
            };

            foreach (var middleware in _fieldMiddleware)
            {
                opts.FieldMiddleware.Use(next => {
                    return ctx => {
                        return middleware.Resolve(ctx, next);
                    };
                });
            }

            foreach (var listener in _listeners)
            {
                opts.Listeners.Add(listener);
            }

            opts.ValidationRules = _validationRules
                .Concat(DocumentValidator.CoreRules())
                .ToList();

            return opts;
        }
    }
}