class Verse {
  final int id;
  final int surahId;
  final int verseNumber;
  final int revelationOrder;
  final String arabicText;
  final String turkishTranslation;
  final String summary;

  Verse({
    required this.id,
    required this.surahId,
    required this.verseNumber,
    required this.revelationOrder,
    required this.arabicText,
    required this.turkishTranslation,
    required this.summary,
  });

  factory Verse.fromJson(Map<String, dynamic> json) {
    return Verse(
      id: json['id'],
      surahId: json['surahId'],
      verseNumber: json['verseNumber'],
      revelationOrder: json['revelationOrder'],
      arabicText: json['arabicText'],
      turkishTranslation: json['turkishTranslation'],
      summary: json['summary'] ?? '',
    );
  }
}
