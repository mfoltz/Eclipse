using System.Collections;

namespace Eclipse.Elements;
internal interface IReactiveElement
{
    void Awake();
    IEnumerator OnUpdate();
}
