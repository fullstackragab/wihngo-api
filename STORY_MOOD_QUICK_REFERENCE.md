# Story Mood - Quick Reference for Mobile Team

## ? TL;DR

- **Mood is OPTIONAL** - never block posting
- **Single selection** - one mood or none
- **Emoji-first UI** - big emojis, small text
- **No mood = no badge** - don't show empty state

---

## ?? 8 Story Moods

| Emoji | Mood | When to Use |
|-------|------|-------------|
| ?? | Love & Bond | Cuddles, trust, affection |
| ?? | New Beginning | First day, adoption, rescue |
| ?? | Progress & Wins | Training success, milestones |
| ?? | Funny Moment | Silly behavior |
| ??? | Peaceful Moment | Calm, beautiful scenes |
| ?? | Loss & Memory | Grief, remembrance |
| ?? | Care & Health | Vet visits, health updates |
| ?? | Daily Life | Normal routines |

---

## ?? UI Implementation

### Mood Selector Screen

```
???????????????????????????????????????
?  Choose a mood (optional)           ?
?  Skip this step if you prefer       ?
?                                      ?
?  [ ?? ]  [ ?? ]  [ ?? ]  [ ?? ]    ?
?  Love    New     Progress  Funny    ?
?                                      ?
?  [ ??? ]  [ ?? ]  [ ?? ]  [ ?? ]    ?
?  Peaceful Loss   Care      Daily    ?
?                                      ?
?  [ Skip / No Mood Selected ]        ?
???????????????????????????????????????
```

### Story Feed Display

**With mood:**
```
???????????????????????????????
? ?? Luna, Charlie            ?
? ?? Love & Bond             ?
? "Today she trusted me..."   ?
? [image]                     ?
???????????????????????????????
```

**Without mood:**
```
???????????????????????????????
? ?? Luna, Charlie            ?
? "Today she trusted me..."   ?
? [image]                     ?
???????????????????????????????
```

---

## ?? Code Examples

### TypeScript/React Native

```typescript
// Mood is optional
interface StoryFormData {
  birdIds: string[];
  content: string;
  mode?: StoryMode | null; // OPTIONAL!
  imageS3Key?: string;
  videoS3Key?: string;
}

// Validation - only validate required fields
function validateStory(data: StoryFormData) {
  if (data.birdIds.length === 0) {
    throw new Error('Select at least one bird');
  }
  if (!data.content.trim()) {
    throw new Error('Enter story content');
  }
  // NOTE: mode is not validated - it's optional!
}

// Display - only show badge if mood exists
function MoodBadge({ mood }: { mood?: StoryMode | null }) {
  if (!mood) return null; // Don't render anything
  
  const moodInfo = MOODS.find(m => m.value === mood);
  return (
    <View style={styles.badge}>
      <Text>{moodInfo.emoji} {moodInfo.label}</Text>
    </View>
  );
}
```

---

## ?? API Quick Reference

### Create Story

```json
// With mood
POST /api/stories
{
  "birdIds": ["uuid"],
  "content": "Story text",
  "mode": "LoveAndBond"
}

// Without mood
POST /api/stories
{
  "birdIds": ["uuid"],
  "content": "Story text",
  "mode": null
}
```

### Response

```json
{
  "storyId": "uuid",
  "content": "Story text",
  "mode": "LoveAndBond", // or null
  "birds": [...]
}
```

---

## ? UX Rules Checklist

When implementing, ensure:

- [ ] Mood selection has clear "Skip" button
- [ ] Posting is NEVER blocked if no mood selected
- [ ] Mood badge only shows when mood exists
- [ ] Emojis are large and prominent
- [ ] Single selection (radio button behavior)
- [ ] No "Please select a mood" error
- [ ] No empty state when mood is null

---

## ?? Common Mistakes to Avoid

? **Don't do this:**
- Making mood required
- Showing "No mood selected" badge
- Validating mood field
- Showing error if mood is null
- Hiding mood selector

? **Do this:**
- Make mood completely optional
- Hide badge when mood is null
- Allow posting without mood
- Show "Skip" option prominently
- Use emoji-first design

---

## ?? Questions?

Check full guide: `MOBILE_STORY_API_GUIDE.md`

---

**Version:** 2.1 | **Status:** Ready for Implementation ?
