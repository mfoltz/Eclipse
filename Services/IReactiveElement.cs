namespace Eclipse.Services;

internal interface IReactiveElement
{
    void Awake();
    System.Collections.IEnumerator OnUpdate();
}
