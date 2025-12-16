-- Demo Data: Arabic Birds and Stories
-- This script populates the database with 5 birds and 15 stories in Arabic
-- Run this after clearing existing demo data

BEGIN;

-- =============================================
-- STEP 1: Clean up existing data (preserve users)
-- =============================================

-- Delete in order of dependencies
DELETE FROM memorial_messages WHERE TRUE;
DELETE FROM memorial_fund_redirections WHERE TRUE;
DELETE FROM support_usage WHERE TRUE;
DELETE FROM support_transactions WHERE TRUE;
DELETE FROM comment_likes WHERE TRUE;
DELETE FROM comments WHERE TRUE;
DELETE FROM story_likes WHERE TRUE;
DELETE FROM stories WHERE TRUE;
DELETE FROM loves WHERE TRUE;
DELETE FROM birds WHERE TRUE;

-- =============================================
-- STEP 2: Create demo user if not exists
-- =============================================

-- Delete demo user if exists and recreate
DELETE FROM users WHERE email = 'demo@wihngo.com';

INSERT INTO users (user_id, name, email, password_hash, bio, email_confirmed, is_account_locked, failed_login_attempts, created_at)
VALUES (
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890'::uuid,
    'أحمد المحمود',
    'demo@wihngo.com',
    '$2a$11$K5Yx9qL8mN3pR7tU2wXyZeABC123DEF456GHI789JKL012MNO345PQR', -- hashed "Demo123!"
    'محب للطيور منذ الصغر. أعتني بطيوري كأنها أفراد من عائلتي. الرياض، السعودية',
    true,  -- email_confirmed
    false, -- is_account_locked
    0,     -- failed_login_attempts
    NOW() - INTERVAL '1 year'
);

-- Get the demo user ID
DO $$
DECLARE
    demo_user_id UUID;
    bird1_id UUID := gen_random_uuid();
    bird2_id UUID := gen_random_uuid();
    bird3_id UUID := gen_random_uuid();
    bird4_id UUID := gen_random_uuid();
    bird5_id UUID := gen_random_uuid();
BEGIN
    -- Get demo user
    SELECT user_id INTO demo_user_id FROM users WHERE email = 'demo@wihngo.com';

    -- =============================================
    -- STEP 3: Create 5 Arabic Birds
    -- =============================================

    -- Bird 1: ببغاء (Parrot) - زمردة
    INSERT INTO birds (
        bird_id, owner_id, name, species, tagline, description, image_url,
        loved_count, supported_count, donation_cents,
        is_premium, is_memorial, max_media_count, last_activity_at, created_at
    ) VALUES (
        bird1_id,
        demo_user_id,
        'زمردة',
        'ببغاء أخضر',
        'ببغاء ذكية تحب الكلام والغناء',
        'زمردة ببغاء خضراء جميلة عمرها ٣ سنوات. تحب التحدث وتردد الكلمات بطريقة مضحكة. تستيقظ مع الفجر وتبدأ بالغناء. طعامها المفضل هو بذور عباد الشمس والفواكه الطازجة. تحب اللعب مع الأطفال وتقليد أصواتهم.',
        'https://images.unsplash.com/photo-1544923246-77307dd628b5?w=400',
        127, 23, 45000,
        false, false, 10, NOW() - INTERVAL '2 days', NOW() - INTERVAL '8 months'
    );

    -- Bird 2: كناري (Canary) - شمس
    INSERT INTO birds (
        bird_id, owner_id, name, species, tagline, description, image_url,
        loved_count, supported_count, donation_cents,
        is_premium, is_memorial, max_media_count, last_activity_at, created_at
    ) VALUES (
        bird2_id,
        demo_user_id,
        'شمس',
        'كناري أصفر',
        'صوته يملأ البيت بالفرح كل صباح',
        'شمس كناري أصفر لامع مثل الذهب. صوته عذب يشبه صوت الناي. يغني أجمل الألحان عند شروق الشمس. عمره سنتان ويتمتع بصحة ممتازة. يحب الاستحمام بالماء البارد ويرفرف بجناحيه فرحاً.',
        'https://images.unsplash.com/photo-1452570053594-1b985d6ea890?w=400',
        89, 15, 28500,
        false, false, 10, NOW() - INTERVAL '5 days', NOW() - INTERVAL '6 months'
    );

    -- Bird 3: حسون (Goldfinch) - ذهبي
    INSERT INTO birds (
        bird_id, owner_id, name, species, tagline, description, image_url,
        loved_count, supported_count, donation_cents,
        is_premium, is_memorial, max_media_count, last_activity_at, created_at
    ) VALUES (
        bird3_id,
        demo_user_id,
        'ذهبي',
        'حسون',
        'ملك الطيور المغردة بألوانه الزاهية',
        'ذهبي حسون نادر بألوان رائعة - أحمر وأصفر وأسود. صوته من أجمل أصوات الطيور في العالم العربي. يحتاج عناية خاصة وطعام متنوع من البذور والخضروات. عمره ٤ سنوات وهو هدية من جدي رحمه الله.',
        'https://images.unsplash.com/photo-1591608971362-f08b2a75731a?w=400',
        203, 41, 89000,
        true, false, 20, NOW() - INTERVAL '1 day', NOW() - INTERVAL '2 years'
    );

    -- Bird 4: بلبل (Bulbul) - نغم
    INSERT INTO birds (
        bird_id, owner_id, name, species, tagline, description, image_url,
        loved_count, supported_count, donation_cents,
        is_premium, is_memorial, max_media_count, last_activity_at, created_at
    ) VALUES (
        bird4_id,
        demo_user_id,
        'نغم',
        'بلبل',
        'طائر الحدائق المحبوب بصوته الشجي',
        'نغم بلبل جميل وجدته صغيراً في حديقة منزلنا قبل ثلاث سنوات. كان مصاباً بجناحه فاعتنيت به حتى شُفي. لكنه تعلق بي ورفض المغادرة. الآن هو جزء من العائلة. يحب التين والعنب والتمر.',
        'https://images.unsplash.com/photo-1620712943543-bcc4688e7485?w=400',
        156, 32, 67500,
        false, false, 10, NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 years'
    );

    -- Bird 5: حمام (Dove) - سلام
    INSERT INTO birds (
        bird_id, owner_id, name, species, tagline, description, image_url,
        loved_count, supported_count, donation_cents,
        is_premium, is_memorial, max_media_count, last_activity_at, created_at
    ) VALUES (
        bird5_id,
        demo_user_id,
        'سلام',
        'حمام أبيض',
        'رمز السلام والمحبة في بيتنا',
        'سلام حمامة بيضاء نقية كالثلج. اشتريتها من سوق الطيور قبل عام. تحب الطيران في فناء المنزل صباحاً ثم تعود إلى قفصها. لها صوت هديل هادئ يبعث الطمأنينة. تأكل الحبوب والذرة.',
        'https://images.unsplash.com/photo-1523608401-53e6ea33c168?w=400',
        78, 12, 19500,
        false, false, 10, NOW() - INTERVAL '7 days', NOW() - INTERVAL '1 year'
    );

    -- =============================================
    -- STEP 4: Create 15 Arabic Stories (3 per bird)
    -- =============================================

    -- Stories for Bird 1: زمردة (Parrot)
    -- StoryMode enum: LoveAndBond=0, NewBeginning=1, ProgressAndWins=2, FunnyMoment=3, PeacefulMoment=4, LossAndMemory=5, CareAndHealth=6, DailyLife=7
    INSERT INTO stories (story_id, bird_id, author_id, content, mode, is_highlighted, like_count, comment_count, created_at)
    VALUES
    (
        gen_random_uuid(), bird1_id, demo_user_id,
        'اليوم زمردة فاجأتني بكلمة جديدة! قالت "صباح الخير" بوضوح تام عندما دخلت الغرفة. ظللت أضحك من الفرحة. يبدو أنها تسمعني كل صباح وأنا أحيي العائلة. الطيور أذكى مما نتصور! 🦜',
        3, false, 34, 8, NOW() - INTERVAL '2 days'
    ),
    (
        gen_random_uuid(), bird1_id, demo_user_id,
        'زيارة الطبيب البيطري اليوم كانت ممتازة. زمردة بصحة جيدة والحمد لله. الدكتور أوصى بإضافة الفواكه الطازجة لنظامها الغذائي. اشتريت لها تفاح وموز وعنب. أكلت التفاح بشهية كبيرة! 🍎',
        6, false, 21, 5, NOW() - INTERVAL '2 weeks'
    ),
    (
        gen_random_uuid(), bird1_id, demo_user_id,
        'اليوم هو عيد ميلاد زمردة الثالث! 🎂 أحضرت لها قفصاً جديداً واسعاً مع ألعاب ملونة. كانت سعيدة جداً وظلت تتأرجح على الأرجوحة الجديدة. ثلاث سنوات من السعادة معها. شكراً يا زمردة على كل الحب. ❤️',
        2, false, 67, 23, NOW() - INTERVAL '1 month'
    );

    -- Stories for Bird 2: شمس (Canary)
    INSERT INTO stories (story_id, bird_id, author_id, content, mode, is_highlighted, like_count, comment_count, created_at)
    VALUES
    (
        gen_random_uuid(), bird2_id, demo_user_id,
        'صباح اليوم كان مميزاً. استيقظت على صوت شمس يغني لحناً جديداً لم أسمعه من قبل. وقفت أستمع له لمدة عشر دقائق كاملة. الكناري حقاً ملك الطيور المغردة. بداية يوم رائعة! ☀️',
        4, false, 45, 12, NOW() - INTERVAL '5 days'
    ),
    (
        gen_random_uuid(), bird2_id, demo_user_id,
        'شمس يحب الاستحمام! وضعت له وعاء ماء صغير فقفز فيه مباشرة وبدأ يرش الماء بجناحيه. المنظر كان مضحكاً جداً. بعد الاستحمام وقف ينفش ريشه تحت أشعة الشمس. سبحان الله على هذا الجمال! 💦',
        3, false, 38, 9, NOW() - INTERVAL '3 weeks'
    ),
    (
        gen_random_uuid(), bird2_id, demo_user_id,
        'لاحظت أن شمس أصبح أقل نشاطاً هذا الأسبوع. أخذته للطبيب البيطري فوراً. الحمد لله اتضح أنه مجرد تغير في الطقس. الطبيب نصحني بتدفئة الغرفة أكثر. الآن عاد لنشاطه وغنائه الجميل. 🏥',
        6, false, 29, 15, NOW() - INTERVAL '2 months'
    );

    -- Stories for Bird 3: ذهبي (Goldfinch)
    INSERT INTO stories (story_id, bird_id, author_id, content, mode, is_highlighted, like_count, comment_count, created_at)
    VALUES
    (
        gen_random_uuid(), bird3_id, demo_user_id,
        'ذهبي اليوم فاز في مسابقة الطيور المغردة المحلية! 🏆 كان صوته الأجمل بين جميع المتسابقين. فخور جداً به. هذه الجائزة مهداة لروح جدي الذي أهداني إياه. رحمك الله يا جدي.',
        2, true, 89, 34, NOW() - INTERVAL '1 day'
    ),
    (
        gen_random_uuid(), bird3_id, demo_user_id,
        'جلسة تدريب اليوم كانت رائعة. ذهبي يتعلم لحناً جديداً من تسجيل صوتي أشغله له. الحسون ذكي جداً في تقليد الأصوات. بعد أسبوع من التدريب أتقن اللحن بنسبة ٧٠٪. مستمرون! 🎵',
        7, false, 52, 11, NOW() - INTERVAL '2 weeks'
    ),
    (
        gen_random_uuid(), bird3_id, demo_user_id,
        'اليوم ذكرى مرور أربع سنوات على وصول ذهبي لبيتنا. أتذكر كأنه أمس عندما أحضره جدي وقال لي: "هذا الطائر سيكون صديقك". كان محقاً. ذهبي ليس مجرد طائر، هو ذكرى حية لجدي الحبيب. 💛',
        0, false, 124, 45, NOW() - INTERVAL '1 month'
    );

    -- Stories for Bird 4: نغم (Bulbul)
    INSERT INTO stories (story_id, bird_id, author_id, content, mode, is_highlighted, like_count, comment_count, created_at)
    VALUES
    (
        gen_random_uuid(), bird4_id, demo_user_id,
        'قصة نغم مميزة. وجدته قبل ثلاث سنوات صغيراً مصاباً في الحديقة. جناحه كان مكسوراً. أخذته للبيت وعالجته بمساعدة طبيب بيطري. بعد شهرين تعافى تماماً لكنه رفض المغادرة. أصبح فرداً من العائلة. 🐦',
        1, false, 78, 28, NOW() - INTERVAL '3 days'
    ),
    (
        gen_random_uuid(), bird4_id, demo_user_id,
        'نغم لديه عادة غريبة وجميلة. كل مساء عند أذان المغرب يبدأ بالتغريد بصوت هادئ وكأنه يشاركنا الأجواء الروحانية. سبحان الله! حتى الطيور تشعر بعظمة هذا الوقت. 🌅',
        4, false, 95, 19, NOW() - INTERVAL '1 week'
    ),
    (
        gen_random_uuid(), bird4_id, demo_user_id,
        'أولادي يحبون نغم كثيراً. اليوم ابنتي الصغيرة رسمت له صورة جميلة وعلقتها بجانب قفصه. نغم وقف يتأمل الصورة وكأنه يفهم! لحظة جميلة جمعت العائلة كلها حول قفصه. 👨‍👩‍👧‍👦',
        0, false, 67, 22, NOW() - INTERVAL '3 weeks'
    );

    -- Stories for Bird 5: سلام (Dove)
    INSERT INTO stories (story_id, bird_id, author_id, content, mode, is_highlighted, like_count, comment_count, created_at)
    VALUES
    (
        gen_random_uuid(), bird5_id, demo_user_id,
        'اليوم سلام طارت لأول مرة في الفناء الجديد! كنت قلقاً أن تهرب لكنها دارت دورة كاملة ثم عادت وحطت على كتفي. لحظة لن أنساها. الثقة بيننا أصبحت قوية جداً. 🕊️',
        2, false, 56, 14, NOW() - INTERVAL '1 week'
    ),
    (
        gen_random_uuid(), bird5_id, demo_user_id,
        'سلام وضعت بيضتين! 🥚🥚 هذه أول مرة. أنا متحمس جداً وقلق في نفس الوقت. جهزت لها مكاناً دافئاً وهادئاً. أدعو الله أن تفقس البيضات بسلامة. سأبقيكم على اطلاع بالتطورات!',
        1, true, 112, 38, NOW() - INTERVAL '3 days'
    ),
    (
        gen_random_uuid(), bird5_id, demo_user_id,
        'صوت هديل سلام في الصباح الباكر يبعث في نفسي السكينة. أجلس بجانب قفصها مع قهوتي وأستمع لها. هذه اللحظات الهادئة قبل بدء يوم العمل هي أجمل لحظات يومي. الحمد لله على نعمة الطيور. ☕',
        4, false, 43, 7, NOW() - INTERVAL '2 months'
    );

END $$;

-- =============================================
-- STEP 5: Verify the data
-- =============================================

-- Show summary
SELECT 'Birds created:' as info, COUNT(*) as count FROM birds
UNION ALL
SELECT 'Stories created:', COUNT(*) FROM stories;

COMMIT;
