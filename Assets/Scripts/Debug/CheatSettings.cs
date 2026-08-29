/// <summary>
/// 개발용 치트/디버그 입력의 단일 스위치.
/// 에디터와 개발 빌드(Development Build)에서만 켜지고, 릴리즈 빌드에서는 완전히 꺼진다.
/// 런타임에 강제로 끄고 싶으면 CheatSettings.Enabled = false 로 대입하면 된다.
/// </summary>
public static class CheatSettings
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static bool Enabled = true;
#else
    public static bool Enabled = false;
#endif
}
