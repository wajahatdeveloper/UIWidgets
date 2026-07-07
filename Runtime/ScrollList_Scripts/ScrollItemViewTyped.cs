namespace UIWidgets
{
    public abstract class ScrollItemView<T> : ScrollItemView, IListItemBinder where T : class
    {
        void IListItemBinder.BindRaw(object data, int index) => Bind((T)data, index);

        public abstract void Bind(T data, int index);
        public abstract void Unbind();
    }
}
