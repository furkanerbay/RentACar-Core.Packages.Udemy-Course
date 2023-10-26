using Core.Persistence.Dynamic;
using Core.Persistence.Paging;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Core.Persistence.Repositories;

public interface IAsyncRepository<TEntity, TEntityId> : IQuery<TEntity>
    where TEntity : Entity<TEntityId>
{
    Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, //Join Desteği - Queryable -> Query geçebilebiriz.
        bool withDeleted = false, //SoftDelete imkanı ile çalışıyoruz sistemimizde veritabanında silinenleri sorgu ile getirme demek. İstersek true olacak.
        bool enableTracking = true,
        CancellationToken cancellationToken = default); // Asenkron implemantasyonlarımızda iptal etme işlemi

    Task<Paginate<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int index = 0, //Kaçıncı sayfa
        int size = 10, //Her sayfada kaçar tane olsun
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    );
    //Dinamik sorgulama
    //Dynamic demek içinde filter ve sortlar olan yapı.
    Task<Paginate<TEntity>> GetListByDynamicAsync(
       DynamicQuery dynamic,
       Expression<Func<TEntity, bool>>? predicate = null,
       Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
       int index = 0,
       int size = 10,
       bool withDeleted = false,
       bool enableTracking = true,
       CancellationToken cancellationToken = default
   );

    Task<bool> AnyAsync(
       Expression<Func<TEntity, bool>>? predicate = null, //Predicate gecmezsek data var mı diye bakar predicate gecer isek kosul var mı dıye bakar.
       bool withDeleted = false,
       bool enableTracking = true,
       CancellationToken cancellationToken = default
   );

    Task<TEntity> AddAsync(TEntity entity);

    Task<ICollection<TEntity>> AddRangeAsync(ICollection<TEntity> entities);

    Task<TEntity> UpdateAsync(TEntity entity);

    Task<ICollection<TEntity>> UpdateRangeAsync(ICollection<TEntity> entities);

    Task<TEntity> DeleteAsync(TEntity entity, bool permanent = false); //Permanent kalıcı demek. SoftDelete mi yapayım yoksa kalıcı mı sileyim ? Veritabanından ucsun mu ucmasın mı ? Buranın false olması kalıcı değil softdelete silindi olarak isaretler.

    Task<ICollection<TEntity>> DeleteRangeAsync(ICollection<TEntity> entities, bool permanent = false);
}
