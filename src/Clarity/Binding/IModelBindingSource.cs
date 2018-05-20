namespace Clarity
{
    public interface IModelBindingSource<TModel>
    {
        TModel CurrentModel { get; set; }
    }
}
