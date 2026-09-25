
namespace BabaTune.Application.Interfaces
{
    public interface IService<T, in TKey> where T : class
    {
        Task AddAsync( T obj );
        Task EditAsync( T obj );
        Task DeleteAsync( T obj );
        Task<T?> GetAsync( TKey id );
        Task<ICollection<T>> GetAllAsync( );
    }
}