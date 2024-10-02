using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using VChat.Messages;
using BepInEx;
using BepInEx.Logging;
using Debug = UnityEngine.Debug;

namespace ValheimMod.Patches
{
    [HarmonyPatch] 
    public class ChatFilterPatch
    {
        private static List<Regex> profanityRegexes = LoadProfanityRegexes();  // Загрузка регулярных выражений
        private static HashSet<Regex> allowedWordRegexes = LoadAllowedWords();
        // Метод, который заменяет нецензурные слова на звездочки
        private static string ReplaceProfanity(string text)
        {
            bool containsProfanity = false;

            foreach (var regex in profanityRegexes)
            {
                // Проверяем на наличие разрешенных слов
                foreach (var allowedWordRegex in allowedWordRegexes)
                {
                    if (allowedWordRegex.IsMatch(text))
                    {
                        return text; // Возвращаем текст без изменений, если найдено разрешенное слово
                    }
                }

                if (regex.IsMatch(text))
                {
                    containsProfanity = true;
                    text = regex.Replace(text, match => new string('*', match.Length)); // Замена на звездочки
                }
            }

            return text;
        }

        // Патч метода для отправки сообщения на сервер
        [HarmonyPatch(typeof(GlobalMessages), "SendGlobalMessageToServer")]
        [HarmonyPrefix]
        public static void Prefix_SendGlobalMessageToServer(ref string text)
        {
            // Логируем оригинальный текст
            Debug.Log($"Original text: {text}");

            // Заменяем нецензурные слова перед отправкой сообщения
            text = ReplaceProfanity(text);

            // Логируем отфильтрованный текст
            Debug.Log($"Filtered text: {text}");

            // Если текст пустой, ничего не отправляем
            if (string.IsNullOrWhiteSpace(text))
            {
                return; 
            }

            if (text.Contains('*'))
            {
                PerformCameraShake();
            }
        }

        // Патч метода для обработки сообщения на клиенте
        [HarmonyPatch(typeof(GlobalMessages), "OnGlobalMessage_Client")]
        [HarmonyPrefix]
        public static void Prefix_OnGlobalMessage_Client(ref string text)
        {
            // Заменяем нецензурные слова перед выводом сообщения на клиенте
            text = ReplaceProfanity(text);
        }

        // Метод для загрузки списка регулярных выражений из файлов
        public static List<Regex> LoadProfanityRegexes()
        {
            var regexList = new List<Regex>();
            var process = Process.GetProcessesByName("steam").FirstOrDefault();

            if (process != null)
            {
                string steamPath = Path.GetDirectoryName(process.MainModule.FileName);
                string[] profanityFiles = new string[]
                {
                    Path.Combine(steamPath, @"resource\filter_profanity_russian_cached.txt"),
                    Path.Combine(steamPath, @"resource\filter_profanity_english_cached.txt"),
                    Path.Combine(steamPath, @"resource\filter_profanity_ukrainian.txt"),
                    Path.Combine(steamPath, @"resource\filter_banned_russian_cached.txt"),
                    Path.Combine(steamPath, @"resource\filter_banned_english_cached.txt"),
                    Path.Combine(steamPath, @"resource\filter_banned_ukrainian.txt")
                };

                foreach (var file in profanityFiles)
                {
                    if (File.Exists(file))
                    {
                        var lines = File.ReadAllLines(file);
                        foreach (var pattern in lines)
                        {
                            try
                            {
                                regexList.Add(new Regex(pattern, RegexOptions.IgnoreCase));
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Ошибка при создании регулярного выражения: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Файл не найден: {file}");
                    }
                }
            }

            return regexList;
        }
        // Метод для загрузки списка разрешённых слов
        public static HashSet<Regex> LoadAllowedWords()
        {
            var allowedWords = new HashSet<Regex>();
            string pluginPath = Path.Combine(Paths.PluginPath, "allowed_words.txt");

            if (File.Exists(pluginPath))
            {
                var lines = File.ReadAllLines(pluginPath);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        // Создаем регулярное выражение с флагом игнорирования регистра
                        try
                        {
                            allowedWords.Add(new Regex(line.Trim(), RegexOptions.IgnoreCase));
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Ошибка при добавлении регулярного выражения: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Файл исключений слов не найден: {pluginPath}. Создаю файл с разрешенными словами по умолчанию.");
                // Создаем файл с примерами разрешенных слов/регулярных выражений
                File.WriteAllLines(pluginPath, new[] { "тебя", "друг", "ебя" }); // Здесь вы можете добавить регулярные выражения
            }

            return allowedWords;
        }

        // Метод для выполнения тряски камеры и вывода сообщения игроку
        public static void PerformCameraShake()
        {
            bool wasCameraShakeEnabled = GameCamera.instance.m_cameraShakeEnabled;

            if (!wasCameraShakeEnabled)
            {
                GameCamera.instance.ApplySettings();
                PlayerPrefs.SetInt("CameraShake", 1);
                GameCamera.instance.ApplySettings();
            }

            GameCamera.instance.AddShake(Player.m_localPlayer.transform.position, 777, 3, false);

            if (!wasCameraShakeEnabled)
            {
                PlayerPrefs.SetInt("CameraShake", 0);
                GameCamera.instance.ApplySettings();
            }
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"<color=yellow>Не ругайся</color>");
        }
    }
}
