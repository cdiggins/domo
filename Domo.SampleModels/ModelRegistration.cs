﻿using System.Drawing;
using Domo;

namespace Domo.SampleModels
{
    #region Application and Infrastructure Domain

    public readonly record struct LogItem(string Category, string Message, string Data, DateTimeOffset Time);

    public readonly record struct ApplicationEvent(string EventName, DateTimeOffset Time);

    public readonly record struct Error(string Category, string Message);

    public readonly record struct User(string Name, DateTimeOffset LogInTime);

    public readonly record struct Folders(string ApplicationFolder, string LogFolder, string TempFolder, string DocumentFiles);

    public readonly record struct Files(string ExecutableFile, string LogFile, string SharedStateFile, string CollaboratorsFile, string PreferencesFiles);

    public readonly record struct UserPreferences(Folders Folders, Files Files, IReadOnlyList<RecentFile> RecentFiles, IReadOnlyList<Macro> Macros);

    public readonly record struct CommandLineArg(string Name, string Value);

    public readonly record struct EnvironmentVariable(string Name, string Value);

    public readonly record struct Command(string Name, string Data);

    public readonly record struct Macro(string Name, IReadOnlyList<Command> Commands);

    public readonly record struct RecentFile(string Path, DateTimeOffset DateOpened);

    public readonly record struct ChangeRecord(string Data, DateTimeOffset DateChanged);

    public readonly record struct KeyBindings(string Key1, string Key2, string Command);

    public readonly record struct Job(string Name, bool Completed, bool Canceled, float Progress, bool Determinate);

    public readonly record struct LastInput(DateTimeOffset Time);

    public enum StatusCode { Good, Warning, Critical }

    public readonly record struct Status(string Message, StatusCode Code);

    public readonly record struct CurrentFile(string FilePath);

    public readonly record struct UndoItem(Guid RepoId, Guid ModelId, string OldValue, string NewValue);

    public readonly record struct UndoState(int CurrentIndex, IReadOnlyList<UndoItem> UndoItems);

    public readonly record struct Freeze();

    #endregion

    #region Presentation Domain

    public readonly record struct ActiveRepo(Guid RepoId);

    public enum ViewTypeEnum
    {
        Canvas,
        Text,
        List
    }

    public readonly record struct ViewSettings(ViewTypeEnum ViewType);

    public readonly record struct ClickAnimation(Point Position, DateTimeOffset Time);

    #endregion

    #region Business Domain

    public record DrawingShape;

    public record Line(Point Start, Point End) : DrawingShape;

    public record Ellipse(Point Center, Size Size) : DrawingShape;

    public record Rectangle(Point Position, Size Size) : DrawingShape;

    public record DrawingCommand;

    public record SetPen(Color Color, float Width) : DrawingCommand;

    public record SetBrush(Color Color) : DrawingCommand;

    public record WriteText(Point Position, string Text) : DrawingCommand;

    public record Select(IModel<DrawingShape> Shape) : DrawingCommand;

    public record Draw(DrawingShape Shape) : DrawingCommand;

    public record Clear() : DrawingCommand;

    public record CanvasClick(Point Position) : DrawingCommand;

    #endregion

    public static class ModelRegistration
    {
        public static IRepository<T> AddTypedRepository<T>(this IDataStore store, IRepository<T> repository)
            => (IRepository<T>)store.AddRepository(repository);

        public static IRepository<T> CreateAggregateRepository<T>(Guid id, Version? version = null, Func<T, bool>? validator = null)
            => new AggregateRepository<T>(id, version ?? new Version(), validator);

        public static IRepository<T> CreateSingletonRepository<T>(Guid id, Version? version = null, T? value = default, Func<T, bool>? validator = null)
            where T: new()
            => new SingletonRepository<T>(id, version ?? new Version(), value ?? new T(), validator);

        public static IRepository<T> RegisterSingleton<T>(this IDataStore store, T? value = default) where T : new() 
            => store.AddTypedRepository(CreateSingletonRepository(Guid.NewGuid(), new Version(), value ?? new T(), null));

        public static IRepository<T> RegisterAggregate<T>(this IDataStore store)
            => store.AddTypedRepository(CreateAggregateRepository<T>(Guid.NewGuid(), new Version(), null));

        public static IDataStore RegisterRepos(IDataStore store)
        {
            store.RegisterAggregate<LogItem>();
            store.RegisterAggregate<Error>();
            store.RegisterSingleton<User>();
            store.RegisterSingleton<UserPreferences>();
            store.RegisterAggregate<CommandLineArg>();
            store.RegisterAggregate<EnvironmentVariable>();
            store.RegisterAggregate<RecentFile>();
            store.RegisterAggregate<ChangeRecord>();
            store.RegisterAggregate<Command>();
            store.RegisterAggregate<Macro>();
            store.RegisterAggregate<Job>();
            
            store.RegisterSingleton<ActiveRepo>();
            store.RegisterSingleton<ViewSettings>();
            store.RegisterAggregate<ClickAnimation>();

            store.RegisterAggregate<DrawingShape>();
            store.RegisterAggregate<DrawingCommand>();
            store.RegisterSingleton<CurrentFile>();
            store.RegisterSingleton<LastInput>();
            store.RegisterAggregate<UndoItem>();
            store.RegisterSingleton<UndoState>();
            return store;

            /*   
    
    
            public readonly record struct BackupSettings(string BackupFilePath, TimeSpan Frequency);
    
            public readonly record struct Command(string Name, string Data);
    
            public readonly record struct KeyBindings(string Key1, string Key2, string Command);
    
            public readonly record struct Job(string Name, bool Completed, bool Canceled, float Progress, bool Determinate);
    
            public readonly record struct LastInput(DateTimeOffset Time);
    
            public enum StatusCode { Good, Warning, Critical }
    
            public readonly record struct Status(string Message, StatusCode Code);
    
            public readonly record struct CuurentFile(string FilePath);
    
            public readonly record struct UndoItem(Guid RepoId, Guid ModelId, string OldValue, string NewValue);
    
            public readonly record struct Freeze();
            */
        }
    }
}