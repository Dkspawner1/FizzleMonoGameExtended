namespace FizzleMonoGameExtended.Scene;

public interface IResolutionDependent
{
    void OnResolutionChanged(int width, int height);
}