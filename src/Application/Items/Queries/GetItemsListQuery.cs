using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Crpg.Application.Common.Interfaces;
using Crpg.Application.Items.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crpg.Application.Items.Queries
{
    public class GetItemsListQuery : IRequest<IList<ItemViewModel>>
    {
        public class Handler : IRequestHandler<GetItemsListQuery, IList<ItemViewModel>>
        {
            private readonly ICrpgDbContext _db;
            private readonly IMapper _mapper;

            public Handler(ICrpgDbContext db, IMapper mapper)
            {
                _db = db;
                _mapper = mapper;
            }

            public async Task<IList<ItemViewModel>> Handle(GetItemsListQuery request, CancellationToken cancellationToken)
            {
                var items = await _db.Items
                    .OrderBy(i => i.Value)
                    .ToListAsync(cancellationToken);

                // can't use ProjectTo https://github.com/dotnet/efcore/issues/20729
                return _mapper.Map<IList<ItemViewModel>>(items);
            }
        }
    }
}
