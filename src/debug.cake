#load "tasks.cake"

Task("Debug")
    .IsDependentOn(KnownTasks.DEFAULT);

RunTarget("Debug");