using UnityEngine.SceneManagement;

namespace Alte.SceneManagement
{
    public static class AlteSceneManagementFramework
    {
        private static SceneNames recentScene;
        private static SceneNames nowScene;
        private static AlteAbstractSceneManager manager;

        public static void Setup()//何らかの方法で最初にこれ呼んでください
        {
            SceneManager.activeSceneChanged += (Scene current, Scene next) => { InitializeScene(); };
            recentScene = 0;
            nowScene = 0;
        }

        public static void SceneTransition()
        {
            SceneNames next = manager.SceneTransition();
            if (next == SceneNames.RecentScene)
            {
                next = recentScene;
            }
            else
            {
                recentScene = nowScene;
                nowScene = next;
            }
            SceneManager.LoadScene(next.ToString());
        }

        private static void InitializeScene()
        {
            SceneRegistry();
            manager.Initialize();
        }

        private static void Register<T>(SceneNames scene) where T : AlteAbstractSceneManager, new()
        {
            if (scene != nowScene) return;
            manager = new T();
        }

        private static void SceneRegistry()//ユーザーさん自身で書き換えてください。例) Register<TestSceneManager>(SceneNames.TestScene);
        {

        }
    }

    public enum SceneNames//一番上に書かれた物が最初のシーンとして扱われます。
    {
        RecentScene//書き換え不可。直前のシーンに戻る
    }

    public abstract class AlteAbstractSceneManager
    {
        public abstract void Initialize();//実質Awakeです

        public abstract SceneNames SceneTransition();//遷移前にしておきたいことと、遷移する対象を戻り値に
    }
}