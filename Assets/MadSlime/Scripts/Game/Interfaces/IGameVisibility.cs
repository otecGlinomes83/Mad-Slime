using System;

namespace Core
{
    public interface IGameVisibility
    {
        event Action Hidden;

        event Action Shown;
    }
}
