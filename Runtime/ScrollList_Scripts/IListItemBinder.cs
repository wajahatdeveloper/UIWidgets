namespace AetherNexus.UIWidgets
{
    public interface IListItemBinder
    {
        void BindRaw(object data, int index);
        void Unbind();
    }
}
