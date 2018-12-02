namespace Ammy
{
    public interface IModelBindingSource<TModel>
    {
        TModel CurrentModel { get; set; }
    }
}
