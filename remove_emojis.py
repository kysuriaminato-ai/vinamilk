import re

def remove_emojis(text):
    # This regex matches most emojis
    emoji_pattern = re.compile(
        u"(\ud83d[\ude00-\ude4f])|"  # emoticons
        u"(\ud83c[\udf00-\uffff])|"  # symbols & pictographs (1 of 2)
        u"(\ud83d[\u0000-\uddff])|"  # symbols & pictographs (2 of 2)
        u"(\ud83d[\ude80-\udeff])|"  # transport & map symbols
        u"(\ud83c[\udde0-\uddff])"  # flags (iOS)
        "+", flags=re.UNICODE)
    
    # Python 3 emoji regex alternative:
    import emoji
    return emoji.replace_emoji(text, replace='')

if __name__ == '__main__':
    try:
        import emoji
    except ImportError:
        import subprocess
        subprocess.check_call(['pip', 'install', 'emoji'])
        import emoji

    filepath = r'c:\Users\MAi THU\OneDrive\HTTT QLNL\vinamilk\BAO_CAO_HE_THONG_THONG_TIN_NHAN_LUC_VINAMILK.md'
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    content = emoji.replace_emoji(content, replace='')
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print("Done removing emojis")
