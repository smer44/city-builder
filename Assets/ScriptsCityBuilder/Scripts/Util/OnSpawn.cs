public interface IOnSpawn<T>
{
    void OnSpawn(T field);
}

public interface OnSpawn<T> : IOnSpawn<T>
{
}
