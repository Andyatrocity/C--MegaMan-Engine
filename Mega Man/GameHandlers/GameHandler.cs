﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaMan.Common;
using Microsoft.Xna.Framework;

namespace MegaMan.Engine
{
    public abstract class GameHandler : IGameplayContainer
    {
        public virtual HandlerInfo Info { get; set; }

        protected Dictionary<string, IHandlerObject> objects = new Dictionary<string, IHandlerObject>();

        public virtual IEntityContainer Entities { get; set; }

        public event Action GameThink;
        public event Action GameAct;
        public event Action GameReact;
        public event Action GameCleanup;
        public event Action<HandlerTransfer> End;
        public event GameRenderEventHandler Draw;

        private bool running;

        public virtual void StartHandler()
        {
            if (Entities == null)
            {
                Entities = new SceneEntities();
            }

            ResumeHandler();
            StartDrawing();
        }

        public virtual void PauseHandler()
        {
            if (!running) return;
            Engine.Instance.GameLogicTick -= Tick;
            Engine.Instance.GameInputReceived -= GameInputReceived;
            running = false;
        }

        public virtual void ResumeHandler()
        {
            if (running) return;
            Engine.Instance.GameLogicTick += Tick;
            Engine.Instance.GameInputReceived += GameInputReceived;
            running = true;
        }

        public virtual void StopHandler()
        {
            PauseHandler();
            StopDrawing();

            Entities.ClearEntities();

            foreach (var obj in objects.Values)
            {
                obj.Stop();
            }
            objects.Clear();
        }

        public virtual void StopDrawing()
        {
            Engine.Instance.GameRender -= GameRender;
        }

        public virtual void StartDrawing()
        {
            Engine.Instance.GameRender += GameRender;
        }

        protected abstract void GameInputReceived(GameInputEventArgs e);

        protected virtual void Tick(GameTickEventArgs e)
        {
            if (GameThink != null) GameThink();
            if (GameAct != null) GameAct();
            if (GameReact != null) GameReact();
            if (GameCleanup != null) GameCleanup();
        }

        protected virtual void GameRender(GameRenderEventArgs e)
        {
            foreach (var obj in objects.Values)
            {
                obj.Draw(e.Layers, e.OpacityColor);
            }

            if (Draw != null) Draw(e);
        }

        protected void Finish(HandlerTransfer transfer)
        {
            if (End != null)
            {
                End(transfer);
            }
        }

        protected virtual void RunCommands(IEnumerable<SceneCommandInfo> commands)
        {
            foreach (var cmd in commands)
            {
                RunCommand(cmd);
            }
        }

        protected virtual void RunCommand(SceneCommandInfo cmd)
        {
            switch (cmd.Type)
            {
                case SceneCommands.PlayMusic:
                    PlayMusicCommand((ScenePlayCommandInfo)cmd);
                    break;

                case SceneCommands.StopMusic:
                    StopMusicCommand((SceneStopMusicCommandInfo)cmd);
                    break;

                case SceneCommands.Add:
                    AddCommand((SceneAddCommandInfo)cmd);
                    break;

                case SceneCommands.Move:
                    MoveCommand((SceneMoveCommandInfo)cmd);
                    break;

                case SceneCommands.Remove:
                    RemoveCommand((SceneRemoveCommandInfo)cmd);
                    break;

                case SceneCommands.Entity:
                    EntityCommand((SceneEntityCommandInfo)cmd);
                    break;

                case SceneCommands.Text:
                    TextCommand((SceneTextCommandInfo)cmd);
                    break;

                case SceneCommands.Fill:
                    FillCommand((SceneFillCommandInfo)cmd);
                    break;

                case SceneCommands.FillMove:
                    FillMoveCommand((SceneFillMoveCommandInfo)cmd);
                    break;

                case SceneCommands.Sound:
                    SoundCommand((SceneSoundCommandInfo)cmd);
                    break;

                case SceneCommands.Next:
                    NextCommand((SceneNextCommandInfo)cmd);
                    break;

                case SceneCommands.Call:
                    CallCommand((SceneCallCommandInfo)cmd);
                    break;

                case SceneCommands.Effect:
                    EffectCommand((SceneEffectCommandInfo)cmd);
                    break;

                case SceneCommands.Condition:
                    ConditionCommand((SceneConditionCommandInfo)cmd);
                    break;
            }
        }

        private void PlayMusicCommand(ScenePlayCommandInfo command)
        {
            if (command.LoopPath != null)
            {
                string intropath = (command.IntroPath != null) ? command.IntroPath.Absolute : null;
                string looppath = (command.LoopPath != null) ? command.LoopPath.Absolute : null;
                Engine.Instance.SoundSystem.LoadMusic(intropath, looppath, 1).Play();
            }
            else
            {
                Engine.Instance.SoundSystem.PlayMusicNSF((uint)command.Track);
            }
        }

        private void StopMusicCommand(SceneStopMusicCommandInfo command)
        {
            Engine.Instance.SoundSystem.StopMusicNsf();
        }

        private void AddCommand(SceneAddCommandInfo command)
        {
            var obj = Info.Objects[command.Object];

            IHandlerObject handler = null;
            if (obj is HandlerSpriteInfo)
            {
                handler = new HandlerSprite(((HandlerSpriteInfo)obj).Sprite, new Point(command.X, command.Y));
            }
            else if (obj is MeterInfo)
            {
                handler = new HandlerMeter(HealthMeter.Create((MeterInfo)obj, false), this);
            }
            handler.Start();
            var name = command.Name ?? Guid.NewGuid().ToString();
            if (!objects.ContainsKey(name)) objects.Add(name, handler);
        }

        private void TextCommand(SceneTextCommandInfo command)
        {
            var obj = new HandlerText(command, Entities);
            obj.Start();
            var name = command.Name ?? Guid.NewGuid().ToString();
            if (!objects.ContainsKey(name)) objects.Add(name, obj);
        }

        private void RemoveCommand(SceneRemoveCommandInfo command)
        {
            if (!objects.ContainsKey(command.Name))
            {
                throw new GameRunException(String.Format("The handler '{0}' referenced an object called '{1}', which doesn't exist.", Info.Name, command.Name));
            }

            objects[command.Name].Stop();
            objects.Remove(command.Name);
        }

        private void EntityCommand(SceneEntityCommandInfo command)
        {
            var entity = GameEntity.Get(command.Placement.entity, this);
            entity.GetComponent<PositionComponent>().SetPosition(command.Placement.screenX, command.Placement.screenY);
            if (!string.IsNullOrEmpty(command.Placement.state))
            {
                entity.SendMessage(new StateMessage(null, command.Placement.state));
            }
            Entities.AddEntity(command.Placement.id ?? Guid.NewGuid().ToString(), entity);
            entity.Start();
        }

        private void FillCommand(SceneFillCommandInfo command)
        {
            Color color = new Color(command.Red, command.Green, command.Blue);

            var obj = new HandlerFill(color, command.X, command.Y, command.Width, command.Height, command.Layer);
            obj.Start();
            var name = command.Name ?? Guid.NewGuid().ToString();
            if (!objects.ContainsKey(name)) objects.Add(name, obj);
        }

        private void CallCommand(SceneCallCommandInfo command)
        {
            var player = Entities.GetEntity("Player");

            if (player != null)
            {
                EffectParser.GetLateBoundEffect(command.Name)(player);
            }
        }

        private void MoveCommand(SceneMoveCommandInfo command)
        {
            HandlerSprite obj = objects[command.Name] as HandlerSprite;
            if (obj != null)
            {
                obj.Move(command.X, command.Y, command.Duration);
                obj.Reset();
            }
        }

        private void FillMoveCommand(SceneFillMoveCommandInfo command)
        {
            HandlerFill obj = objects[command.Name] as HandlerFill;
            if (obj != null) obj.Move(command.X, command.Y, command.Width, command.Height, command.Duration);
        }

        private void SoundCommand(SceneSoundCommandInfo command)
        {
            Engine.Instance.SoundSystem.PlaySfx(command.SoundInfo.Name);
        }

        private void NextCommand(SceneNextCommandInfo command)
        {
            if (command.NextHandler != null)
            {
                Finish(command.NextHandler);
            }
        }

        private void EffectCommand(SceneEffectCommandInfo command)
        {
            var entity = Entities.GetEntity(command.EntityId);

            var effect = EffectParser.GetOrLoadEffect(command.GeneratedName, command.EffectNode);
            effect(entity);
        }

        private void ConditionCommand(SceneConditionCommandInfo command)
        {
            var condition = EffectParser.ParseCondition(command.ConditionExpression);
            var entity = this.Entities.GetEntities(command.ConditionEntity).FirstOrDefault();

            if (condition(entity))
            {
                RunCommands(command.Commands);
            }
        }
    }
}
