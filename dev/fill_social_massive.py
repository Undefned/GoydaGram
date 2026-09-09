#!/usr/bin/env python3
"""
Заполнение Social Service данными на основе существующих пользователей и видео
С ПОДДЕРЖКОЙ АУТЕНТИФИКАЦИИ MONGODB
"""

import pymongo
import random
import uuid
from datetime import datetime, timedelta
import requests
import json
from typing import List, Dict, Any
import os

# ==================== КОНФИГУРАЦИЯ ====================

CONFIG = {
    # MongoDB подключение с аутентификацией
    "mongo_host": "localhost",
    "mongo_port": 27017,
    "mongo_db": "socialdb",
    "mongo_user": "goydagram_mongo",
    "mongo_password": "16f63b35bf972a8c1eee478351a6c7a",
    "mongo_auth_db": "socialdb",
    
    # Альтернативно - можно использовать строку подключения
    # "mongo_uri": "mongodb://root:example@localhost:27017",
    
    "social_api_url": "http://localhost:5003/api",
    "mode": "direct",  # 'direct' или 'api'
    "batch_size": 500,
    "clean_before": True,
    "use_existing_counts": True,
}

# ==================== ВАШИ ID ИЗ SQL ====================

USERS = [
    {
        "id": "585c34fc-5261-57cf-a48e-5acb284628bf",
        "username": "ddenis22072006@gmail.com",
        "role": "Admin"
    },
    {
        "id": "585ae9d3-7cc8-5564-867d-0bd5d99647a0",
        "username": "alice",
        "role": "User"
    },
    {
        "id": "c14eed81-5191-5f7b-920f-f94a8705fb3a",
        "username": "bob",
        "role": "User"
    },
    {
        "id": "f493520e-bc84-5467-aac6-b5a00d4ec20e",
        "username": "carol",
        "role": "User"
    },
    {
        "id": "443adbb9-01a7-517f-98d7-18f84d88d7a8",
        "username": "dave",
        "role": "User"
    },
    {
        "id": "b9289193-7755-59ca-9466-a7b64e45b14d",
        "username": "erin",
        "role": "User"
    },
    {
        "id": "6e05602e-4bdd-5d7a-8eaa-6cbad25834a9",
        "username": "frank",
        "role": "User"
    },
    {
        "id": "fdd45d33-916f-5dee-901b-4714483480a8",
        "username": "grace",
        "role": "User"
    }
]

VIDEOS = [
    {
        "id": "a8743f1e-e91a-5f32-b20f-9303727ce688",
        "user_id": "6e05602e-4bdd-5d7a-8eaa-6cbad25834a9",
        "title": "Building a YARP API Gateway from scratch",
        "views": 2002,
        "likes": 33
    },
    {
        "id": "1d053079-282f-5e3d-a519-02b4e5140b22",
        "user_id": "6e05602e-4bdd-5d7a-8eaa-6cbad25834a9",
        "title": "React Query in 10 minutes",
        "views": 1789,
        "likes": 88
    },
    {
        "id": "2039062e-79dc-5768-8c1a-3412653b9f85",
        "user_id": "6e05602e-4bdd-5d7a-8eaa-6cbad25834a9",
        "title": "Why I switched to Go for backend services",
        "views": 35763,
        "likes": 1407
    },
    {
        "id": "d224f66f-5c5f-5837-abe0-135d0192d9cd",
        "user_id": "585c34fc-5261-57cf-a48e-5acb284628bf",
        "title": "GoydaGram devlog #1: the auth flow",
        "views": 475,
        "likes": 29
    },
    {
        "id": "d5002ef8-d34f-52d9-b764-13f430616670",
        "user_id": "585c34fc-5261-57cf-a48e-5acb284628bf",
        "title": "GoydaGram devlog #2: HLS transcoding",
        "views": 22348,
        "likes": 658
    },
    {
        "id": "00fecb22-453c-5615-8801-8b8fac53b173",
        "user_id": "585c34fc-5261-57cf-a48e-5acb284628bf",
        "title": "Debugging a Docker Compose healthcheck bug",
        "views": 10,
        "likes": 0
    },
    {
        "id": "438f7e4e-a980-54e2-a5bc-ba89f544ae4f",
        "user_id": "585ae9d3-7cc8-5564-867d-0bd5d99647a0",
        "title": "Try not to laugh challenge",
        "views": 22591,
        "likes": 1180
    },
    {
        "id": "716e20b7-6906-58da-a343-29410d071fdb",
        "user_id": "585ae9d3-7cc8-5564-867d-0bd5d99647a0",
        "title": "Cats being cats compilation",
        "views": 30158,
        "likes": 1433
    },
    {
        "id": "9203e321-e076-525b-ab40-f28be77bf54b",
        "user_id": "585ae9d3-7cc8-5564-867d-0bd5d99647a0",
        "title": "Late night rant about traffic",
        "views": 2,
        "likes": 0
    },
    {
        "id": "9b12a732-4d24-5ca6-b453-5390e93ce18f",
        "user_id": "c14eed81-5191-5f7b-920f-f94a8705fb3a",
        "title": "Ranked grind highlights",
        "views": 23750,
        "likes": 1197
    },
    {
        "id": "73d4bc28-427a-50ee-80ec-8eab0b57b1a8",
        "user_id": "c14eed81-5191-5f7b-920f-f94a8705fb3a",
        "title": "New patch reaction",
        "views": 43386,
        "likes": 1125
    },
    {
        "id": "1d7f985d-9358-5b88-a1be-91d2db9b3554",
        "user_id": "c14eed81-5191-5f7b-920f-f94a8705fb3a",
        "title": "Speedrun world record attempt",
        "views": 7,
        "likes": 0
    },
    {
        "id": "2203ae4e-3b94-5149-9a74-2a81f39bcf72",
        "user_id": "f493520e-bc84-5467-aac6-b5a00d4ec20e",
        "title": "Street food tour: Bangkok",
        "views": 41710,
        "likes": 2852
    },
    {
        "id": "85025d45-29e6-54e8-9579-acc3bad3fd7a",
        "user_id": "f493520e-bc84-5467-aac6-b5a00d4ec20e",
        "title": "5-minute pasta that doesn't suck",
        "views": 13780,
        "likes": 784
    },
    {
        "id": "0e1a4f0d-cfb3-5932-9cdf-dbd22307a833",
        "user_id": "f493520e-bc84-5467-aac6-b5a00d4ec20e",
        "title": "Backpacking the Balkans on $30/day",
        "views": 42519,
        "likes": 637
    },
    {
        "id": "caa04363-6552-529a-9fe1-e59043f01bd4",
        "user_id": "b9289193-7755-59ca-9466-a7b64e45b14d",
        "title": "10-minute morning mobility routine",
        "views": 47834,
        "likes": 1298
    },
    {
        "id": "f449a689-d25a-5863-be29-2838ed4cfb20",
        "user_id": "b9289193-7755-59ca-9466-a7b64e45b14d",
        "title": "Common squat mistakes",
        "views": 41993,
        "likes": 2442
    },
    {
        "id": "af00f26d-ef5f-552c-aa0d-082ddd8ba17f",
        "user_id": "fdd45d33-916f-5dee-901b-4714483480a8",
        "title": "Watercolor basics for beginners",
        "views": 3715,
        "likes": 96
    },
    {
        "id": "9f40f01f-a7ab-5a2c-8f4d-cf4673f61361",
        "user_id": "fdd45d33-916f-5dee-901b-4714483480a8",
        "title": "Sketchbook tour",
        "views": 12,
        "likes": 0
    },
    {
        "id": "c213c563-69a1-502b-9bd1-694b3f328d3e",
        "user_id": "443adbb9-01a7-517f-98d7-18f84d88d7a8",
        "title": "Untitled upload",
        "views": 47099,
        "likes": 1508
    }
]

# ==================== ПОДКЛЮЧЕНИЕ К MONGODB ====================

def get_mongo_client():
    """Подключение к MongoDB с аутентификацией"""
    try:
        # Способ 1: Через URI
        mongo_uri = os.getenv("MONGO_URI")
        if mongo_uri:
            client = pymongo.MongoClient(mongo_uri)
            print(f"✅ Подключено через URI: {mongo_uri}")
            return client
        
        # Способ 2: Через параметры
        if CONFIG.get("mongo_user") and CONFIG.get("mongo_password"):
            client = pymongo.MongoClient(
                host=CONFIG["mongo_host"],
                port=CONFIG["mongo_port"],
                username=CONFIG["mongo_user"],
                password=CONFIG["mongo_password"],
                authSource=CONFIG.get("mongo_auth_db", "admin"),
                authMechanism='SCRAM-SHA-256'
            )
            print(f"✅ Подключено с аутентификацией: {CONFIG['mongo_user']}@{CONFIG['mongo_host']}")
            return client
        
        # Способ 3: Без аутентификации
        client = pymongo.MongoClient(CONFIG["mongo_host"], CONFIG["mongo_port"])
        print("⚠️ Подключено БЕЗ аутентификации (может не работать)")
        return client
        
    except Exception as e:
        print(f"❌ Ошибка подключения к MongoDB: {e}")
        print("\n💡 ПРОВЕРЬТЕ:")
        print("   1. Запущен ли MongoDB?")
        print("   2. Правильные ли логин/пароль?")
        print("   3. В Docker Compose: mongodb://root:example@localhost:27017")
        print("   4. Попробуйте: MONGO_URI='mongodb://root:example@localhost:27017' python script.py")
        raise

# ==================== ГЕНЕРАТОРЫ ====================

def generate_comment_texts():
    """Генерирует тексты комментариев"""
    templates = [
        "Отличное видео! {}",
        "Круто, очень понравилось! {}",
        "Спасибо за контент! {}",
        "Лучшее видео за сегодня! {}",
        "🔥 огонь! {}",
        "😂 угар! {}",
        "💯 топ! {}",
        "👍 лайк! {}",
        "😮 вау! {}",
        "❤️ класс! {}",
        "Interesting video! {}",
        "Nice content! {}",
        "Awesome! {}",
        "Keep it up! {}",
        "Love it! {}",
        "Great job! {}",
        "So cool! {}",
        "Amazing! {}",
        "Super! {}",
        "Fantastic! {}"
    ]
    emojis = ["", "🔥", "😂", "💯", "❤️", "👍", "😮", "👏", "🎉", "✨", "⭐", "🌟"]
    template = random.choice(templates)
    emoji = random.choice(emojis)
    return template.format(emoji)

def generate_interest_tags():
    """Генерирует теги для интересов"""
    all_tags = [
        "funny", "music", "dance", "gaming", "sports", "food", "travel",
        "fashion", "beauty", "tech", "science", "education", "news", "entertainment",
        "comedy", "drama", "action", "adventure", "romance", "thriller", "horror",
        "documentary", "animation", "podcast", "vlog", "tutorial", "review",
        "unboxing", "challenge", "prank", "reaction", "compilation", "meme"
    ]
    count = random.randint(3, 8)
    return random.sample(all_tags, min(count, len(all_tags)))

def generate_interests_with_weights():
    """Генерирует интересы с весами"""
    tags = generate_interest_tags()
    interests = []
    for tag in tags:
        weight = round(random.uniform(0.3, 1.0), 2)
        interests.append({"tag": tag, "weight": weight})
    return sorted(interests, key=lambda x: x["weight"], reverse=True)

def random_date(start, end):
    """Генерирует случайную дату"""
    return start + timedelta(
        seconds=random.randint(0, int((end - start).total_seconds()))
    )

# ==================== ОСНОВНЫЕ ФУНКЦИИ ====================

def clean_database(db):
    """Очищает коллекции"""
    print("🧹 Очистка базы данных...")
    collections = ["likes", "comments", "views", "user_interests"]
    for col in collections:
        try:
            result = db[col].delete_many({})
            print(f"   {col}: удалено {result.deleted_count} записей")
        except Exception as e:
            print(f"   {col}: ошибка - {e}")
    print("✅ База очищена\n")

def generate_likes():
    """Генерирует лайки для всех видео"""
    print("📝 Генерация лайков...")
    likes = []
    user_ids = [u["id"] for u in USERS]
    
    for video in VIDEOS:
        if CONFIG["use_existing_counts"] and video.get("likes"):
            max_likes = min(video["likes"], len(user_ids))
        else:
            max_likes = random.randint(5, min(50, len(user_ids)))
        
        users_for_video = random.sample(user_ids, min(max_likes, len(user_ids)))
        
        for user_id in users_for_video:
            likes.append({
                "video_id": video["id"],
                "user_id": user_id,
                "created_at": random_date(
                    datetime.now() - timedelta(days=30),
                    datetime.now()
                )
            })
    
    return likes

def generate_comments():
    """Генерирует комментарии для всех видео"""
    print("📝 Генерация комментариев...")
    comments = []
    user_ids = [u["id"] for u in USERS]
    all_comment_texts = []
    
    for _ in range(200):
        all_comment_texts.append(generate_comment_texts())
    
    for video in VIDEOS:
        if CONFIG["use_existing_counts"] and video.get("views"):
            max_comments = min(
                int(video["views"] / 100) + 2,
                random.randint(3, 20)
            )
        else:
            max_comments = random.randint(2, 15)
        
        users_for_video = random.sample(
            user_ids, 
            min(max_comments, len(user_ids))
        )
        
        for user_id in users_for_video:
            parent_id = None
            if random.random() < 0.05 and len(comments) > 0:
                parent = random.choice(comments)
                parent_id = parent.get("_id", parent.get("id"))
            
            comments.append({
                "video_id": video["id"],
                "user_id": user_id,
                "text": random.choice(all_comment_texts),
                "parent_id": str(parent_id) if parent_id else None,
                "created_at": random_date(
                    datetime.now() - timedelta(days=30),
                    datetime.now()
                ),
                "updated_at": datetime.now()
            })
    
    return comments

def generate_views():
    """Генерирует просмотры для всех видео"""
    print("📝 Генерация просмотров...")
    views = []
    user_ids = [u["id"] for u in USERS]
    
    for video in VIDEOS:
        if CONFIG["use_existing_counts"] and video.get("views"):
            max_views = min(video["views"], len(user_ids) * 2)
        else:
            max_views = random.randint(10, min(100, len(user_ids) * 2))
        
        users_for_video = random.sample(
            user_ids,
            min(max_views, len(user_ids))
        )
        
        for user_id in users_for_video:
            views.append({
                "video_id": video["id"],
                "user_id": user_id,
                "created_at": random_date(
                    datetime.now() - timedelta(days=30),
                    datetime.now()
                )
            })
    
    return views

def generate_interests():
    """Генерирует интересы для всех пользователей"""
    print("📝 Генерация интересов пользователей...")
    interests = []
    
    for user in USERS:
        if user["role"] == "Admin":
            interest_count = random.randint(5, 10)
        else:
            interest_count = random.randint(3, 7)
        
        tags = random.sample([
            "tech", "programming", "gaming", "music", "dance", "funny",
            "sports", "food", "travel", "fashion", "science", "education",
            "entertainment", "comedy", "action", "adventure", "vlog", "tutorial"
        ], min(interest_count, 18))
        
        user_interests = []
        for tag in tags:
            weight = round(random.uniform(0.3, 1.0), 2)
            user_interests.append({"tag": tag, "weight": weight})
        
        user_interests = sorted(user_interests, key=lambda x: x["weight"], reverse=True)
        
        interests.append({
            "user_id": user["id"],
            "interests": user_interests,
            "updated_at": random_date(
                datetime.now() - timedelta(days=10),
                datetime.now()
            )
        })
    
    return interests

def batch_insert(db, collection, data, batch_size=500):
    """Вставляет данные пачками"""
    if not data:
        print(f"   {collection}: нет данных")
        return 0
    
    total = len(data)
    inserted = 0
    
    for i in range(0, total, batch_size):
        batch = data[i:i+batch_size]
        try:
            db[collection].insert_many(batch)
            inserted += len(batch)
            progress = (inserted / total) * 100
            print(f"\r   {collection}: {inserted}/{total} ({progress:.1f}%)", end="")
        except Exception as e:
            print(f"\n⚠️ Ошибка вставки в {collection}: {e}")
            # Пробуем по одной записи
            print(f"   Повторная вставка по одной...")
            for item in batch:
                try:
                    db[collection].insert_one(item)
                    inserted += 1
                except Exception as e2:
                    print(f"   ❌ Ошибка: {e2}")
    
    print(f"\r   {collection}: {inserted}/{total} (100%) ✅")
    return inserted

# ==================== MAIN ====================

def main():
    print("=" * 60)
    print("🚀 SOCIAL SERVICE DATA FILLER")
    print("=" * 60)
    
    # Подключение к MongoDB
    print("\n📡 Подключение к MongoDB...")
    client = get_mongo_client()
    db = client[CONFIG["mongo_db"]]
    
    # Проверка подключения
    try:
        db.command("ping")
        print("✅ MongoDB доступен")
    except Exception as e:
        print(f"❌ Ошибка подключения: {e}")
        return
    
    print(f"\n📊 Статистика:")
    print(f"   Пользователей: {len(USERS)}")
    print(f"   Видео: {len(VIDEOS)}")
    print(f"   Режим: {CONFIG['mode'].upper()}")
    print(f"   База: {CONFIG['mongo_db']}")
    
    # Очистка
    if CONFIG["clean_before"]:
        clean = input("\n🗑️ Очистить базу перед заполнением? (y/n): ").lower()
        if clean == 'y':
            clean_database(db)
    
    start_time = datetime.now()
    
    if CONFIG["mode"] == "direct":
        # Генерируем данные
        print("\n📊 Генерация данных...")
        likes = generate_likes()
        comments = generate_comments()
        views = generate_views()
        interests = generate_interests()
        
        print(f"\n📈 Сгенерировано:")
        print(f"   Лайки: {len(likes):,}")
        print(f"   Комментарии: {len(comments):,}")
        print(f"   Просмотры: {len(views):,}")
        print(f"   Интересы: {len(interests):,}")
        
        # Вставляем
        print("\n💾 Вставка в MongoDB...")
        batch_insert(db, "likes", likes, CONFIG["batch_size"])
        batch_insert(db, "comments", comments, CONFIG["batch_size"])
        batch_insert(db, "views", views, CONFIG["batch_size"])
        batch_insert(db, "user_interests", interests, CONFIG["batch_size"])
    
    elapsed = (datetime.now() - start_time).total_seconds()
    
    # Итоговая статистика
    print("\n" + "=" * 60)
    print("📊 ИТОГОВАЯ СТАТИСТИКА")
    print("=" * 60)
    
    for col in ["likes", "comments", "views", "user_interests"]:
        try:
            count = db[col].count_documents({})
            print(f"{col:20}: {count:>10,}")
        except Exception as e:
            print(f"{col:20}: ❌ {e}")
    
    print(f"\n⏱️ Время выполнения: {elapsed:.2f} секунд")
    print("\n✅ Готово!")

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\n⏹️ Прервано пользователем")
    except Exception as e:
        print(f"\n❌ Ошибка: {e}")
        import traceback
        traceback.print_exc()