// https://docs.unity3d.com/ja/2018.4/Manual/webgl-interactingwithbrowserscripting.html
// https://docs.unity3d.com/ja/2018.4/Manual/webgl-debugging.html

mergeInto(LibraryManager.library, {
  SyncFS: function () {
    FS.syncfs(false, function (err) {
      console.log('Error: syncfs failed!');
    });
  },
});