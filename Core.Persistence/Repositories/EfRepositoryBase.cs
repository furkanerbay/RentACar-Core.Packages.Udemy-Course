using Core.Persistence.Dynamic;
using Core.Persistence.Paging;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core.Persistence.Repositories;

public class EfRepositoryBase<TEntity, TEntityId, TContext> : IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>//1- Hangi entityle calısıyorum 2- Bunun id'sinin veri tipi 3- context
    where TEntity : Entity<TEntityId>
    where TContext : DbContext
{
    protected readonly TContext Context;
    public EfRepositoryBase(TContext context)
    {
        Context = context;
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        entity.CreatedDate = DateTime.UtcNow;
        await Context.AddAsync(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public async Task<ICollection<TEntity>> AddRangeAsync(ICollection<TEntity> entities)
    {
        foreach (TEntity entity in entities)
            entity.CreatedDate = DateTime.UtcNow;
        await Context.AddRangeAsync(entities);
        await Context.SaveChangesAsync();
        return entities;
    }
    //Any data var mı yok mu bize onu dönecek belirttiğimiz preedicate'a göre tahmin-filtre değerine göre
    //Entity framework global filters -> Query Filters'da olabilir. Ortak Query
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)//Tracking navigasyon propertyleri için -> Aktif değilse false ise
            queryable.AsNoTracking();
        if (withDeleted)//Eğer withDeleted'ı true'ya cevirmiş ise
            queryable = queryable.IgnoreQueryFilters();//Global filtrem olacak.
        if (predicate != null) //Predicate null'dan farklı birşey ise
            queryable = queryable.Where(predicate);
        return await queryable.AnyAsync(cancellationToken);
    }
    //Permanent'ı kalıcı mı yapıcam soft delete'mi yapıcam ? 
    //Kalıcı ise sileceksin. Soft Delete ise update ile tarihini değiştirecek.
    public async Task<TEntity> DeleteAsync(TEntity entity, bool permanent = false)
    {
        await SetEntityAsDeletedAsync(entity, permanent); //Buradaki yapı nesnenin silinecek mi güncellenecek mi ona bakacak.
        await Context.SaveChangesAsync();
        return entity;
    }
    public async Task<ICollection<TEntity>> DeleteRangeAsync(ICollection<TEntity> entities, bool permanent = false)
    {
        await SetEntityAsDeletedAsync(entities, permanent); //Buradaki yapı nesnenin silinecek mi güncellenecek mi ona bakacak.
        await Context.SaveChangesAsync();
        return entities;
    }

    public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        return await queryable.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<Paginate<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? predicate = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, int index = 0, int size = 10, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking) //Açık değilse
            queryable = queryable.AsNoTracking(); //Kapay
        if (include != null) //Include ıslemı var mı joın var mı
            queryable = include(queryable); //Varsa ona ekle gönderilen joini sorguya ekle
        if (withDeleted) //with deleted mi?
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null) // Where
            queryable = queryable.Where(predicate);
        if (orderBy != null) // Order By'larımızı dahil edip
            return await orderBy(queryable).ToPaginateAsync(index, size, cancellationToken); //Paginate extension ile pagination desteği sağladık.
        return await queryable.ToPaginateAsync(index, size, cancellationToken);
    }

    public async Task<Paginate<TEntity>> GetListByDynamicAsync(DynamicQuery dynamic, Expression<Func<TEntity, bool>>? predicate = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, int index = 0, int size = 10, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Query().ToDynamic(dynamic);
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return await queryable.ToPaginateAsync(index, size, cancellationToken);
    }

    public IQueryable<TEntity> Query() => Context.Set<TEntity>(); //Burası EF'de bizim ilgili domain nesnesine attech olmamız yani onu context.set etmemiz.

    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        entity.UpdatedDate = DateTime.UtcNow;
        Context.Update(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public async Task<ICollection<TEntity>> UpdateRangeAsync(ICollection<TEntity> entities)
    {
        foreach (TEntity entity in entities)
            entity.UpdatedDate = DateTime.UtcNow;
        Context.UpdateRange(entities);
        await Context.SaveChangesAsync();
        return entities;
    }
    //Protected yazılmasının sebebi yarın öbür gün bu EfEntityRepositoryBase'ı de birisinin base almasını
    //Sağlayıp onu değiştirmek isterse burada değişiklik yapabilmesi için.
    protected async Task SetEntityAsDeletedAsync(TEntity entity, bool permanent)
    {
        if (!permanent) // Eğer kalıcı değil ise soft delete ise
        {
            CheckHasEntityHaveOneToOneRelation(entity); //One To One ilişki olayı var mı
            await setEntityAsSoftDeletedAsync(entity); // SoftDelete'e çevirecek metod.
        }
        else
        {
            Context.Remove(entity);
        }
    }
    //Entity Framework'un bize verdiği data meta ile bu hareketi gerçekleştirebiliyoruz.
    protected void CheckHasEntityHaveOneToOneRelation(TEntity entity)
    {
        bool hasEntityHaveOneToOneRelation =
            Context //İlgili Context'te
                .Entry(entity) //İlgili entity'nin
                .Metadata.GetForeignKeys() //Metadatasının Foreign Keylerini al.
                .All( // Her bir foreign key için 
                    x =>
                        x.DependentToPrincipal?.IsCollection == true
                        || x.PrincipalToDependent?.IsCollection == true
                        || x.DependentToPrincipal?.ForeignKey.DeclaringEntityType.ClrType == entity.GetType() //O ForeignKey'in declaration type'ına bak. Bunlarda one to one ilişki tespit et ona göre bana bu işlemin sonucunu dön.
                ) == false; //Bu ilişkilerin içerisinde koleksiyon yoksa 1-1 dir.
        if (hasEntityHaveOneToOneRelation) // Eğer One To One ilişkisi varsa
            throw new InvalidOperationException(
                "Entity has one-to-one relationship. Soft Delete causes problems if you try to create entry again by same foreign key."
            );//Kullanıcıyı one to one ilişkisi var diye uyarıyoruz.
    }

    //Burada bu entity'nin deletedDate'i var mı yok mu ona bakacağız.
    private async Task setEntityAsSoftDeletedAsync(IEntityTimestamps entity)
    {
        if (entity.DeletedDate.HasValue) //DeletedDate'in değeri var mı
            return; //Değeri varsa döndür.
        entity.DeletedDate = DateTime.UtcNow; //DeletedDate'i yoksa oluştur.

        //Bunun dışında bizim foreignKey'leri ilişkili oldugu bir sürü yer var.
        //Bütün ilişkisel yerleri bulup ürünü soft delete yaptıgımızda urune ait olan tum satıslarında deleted hale cekılmesı gerekıyor.
        //Burada hem reflection hem entityFramework'un bize sagladıgı etkılerden yararlanıyoruz.
        var navigations = Context
            .Entry(entity)
            .Metadata.GetNavigations()
            .Where(x => x is { IsOnDependent: false, ForeignKey.DeleteBehavior: DeleteBehavior.ClientCascade or DeleteBehavior.Cascade })
            .ToList();
        foreach (INavigation? navigation in navigations)
        {
            if (navigation.TargetEntityType.IsOwned())
                continue;
            if (navigation.PropertyInfo == null)
                continue;

            object? navValue = navigation.PropertyInfo.GetValue(entity);
            if (navigation.IsCollection)
            {
                if (navValue == null)
                {
                    IQueryable query = Context.Entry(entity).Collection(navigation.PropertyInfo.Name).Query();
                    navValue = await GetRelationLoaderQuery(query, navigationPropertyType: navigation.PropertyInfo.GetType()).ToListAsync();
                    if (navValue == null)
                        continue;
                }

                foreach (IEntityTimestamps navValueItem in (IEnumerable)navValue)
                    await setEntityAsSoftDeletedAsync(navValueItem);
            }
            else
            {
                if (navValue == null)
                {
                    IQueryable query = Context.Entry(entity).Reference(navigation.PropertyInfo.Name).Query();
                    navValue = await GetRelationLoaderQuery(query, navigationPropertyType: navigation.PropertyInfo.GetType())
                        .FirstOrDefaultAsync();
                    if (navValue == null)
                        continue;
                }

                await setEntityAsSoftDeletedAsync((IEntityTimestamps)navValue);
            }
        }

        Context.Update(entity); //Dikkat UPDATE ediyor SoftDelete'de.
    }
    //Bütün relation'ları bulmaya yarayacak.
    protected IQueryable<object> GetRelationLoaderQuery(IQueryable query, Type navigationPropertyType)
    {
        Type queryProviderType = query.Provider.GetType();
        MethodInfo createQueryMethod =
            queryProviderType
                .GetMethods()
                .First(m => m is { Name: nameof(query.Provider.CreateQuery), IsGenericMethod: true })
                ?.MakeGenericMethod(navigationPropertyType)
            ?? throw new InvalidOperationException("CreateQuery<TElement> method is not found in IQueryProvider.");
        var queryProviderQuery =
            (IQueryable<object>)createQueryMethod.Invoke(query.Provider, parameters: new object[] { query.Expression })!;
        return queryProviderQuery.Where(x => !((IEntityTimestamps)x).DeletedDate.HasValue);
    }

    protected async Task SetEntityAsDeletedAsync(IEnumerable<TEntity> entities, bool permanent)
    {
        foreach (TEntity entity in entities)
            await SetEntityAsDeletedAsync(entity, permanent);
    }

    public TEntity? Get(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, bool withDeleted = false, bool enableTracking = true)
    {
        throw new NotImplementedException();
    }

    public Paginate<TEntity> GetList(Expression<Func<TEntity, bool>>? predicate = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, int index = 0, int size = 10, bool withDeleted = false, bool enableTracking = true)
    {
        throw new NotImplementedException();
    }

    public Paginate<TEntity> GetListByDynamic(DynamicQuery dynamic, Expression<Func<TEntity, bool>>? predicate = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, int index = 0, int size = 10, bool withDeleted = false, bool enableTracking = true)
    {
        throw new NotImplementedException();
    }

    public bool Any(Expression<Func<TEntity, bool>>? predicate = null, bool withDeleted = false, bool enableTracking = true)
    {
        throw new NotImplementedException();
    }

    public TEntity Add(TEntity entity)
    {
        throw new NotImplementedException();
    }

    public ICollection<TEntity> AddRange(ICollection<TEntity> entities)
    {
        throw new NotImplementedException();
    }

    public TEntity Update(TEntity entity)
    {
        throw new NotImplementedException();
    }

    public ICollection<TEntity> UpdateRange(ICollection<TEntity> entities)
    {
        throw new NotImplementedException();
    }

    public TEntity Delete(TEntity entity, bool permanent = false)
    {
        throw new NotImplementedException();
    }

    public ICollection<TEntity> DeleteRange(ICollection<TEntity> entity, bool permanent = false)
    {
        throw new NotImplementedException();
    }
}
