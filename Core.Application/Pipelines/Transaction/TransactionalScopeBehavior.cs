using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Core.Application.Pipelines.Transaction;
//Bunun bir pipeline olabilmesi için request ve response türü verilmesi gerekiyor.
public class TransactionalScopeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ITransactionalRequest
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
		using TransactionScope transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        TResponse response;
		try
		{
			response = await next();
			transactionScope.Complete();
		}
		catch (Exception)
		{
			transactionScope.Dispose(); //Geri al iptal et.
			throw;	//Ve bir tane transactional hatası fırlat.
		}

		return response;
    }
}
