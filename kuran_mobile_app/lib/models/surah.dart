class Surah {
  final int id;
  final int surahNumber;
  final String name;
  final String englishName;
  final String meaning;
  final int revelationOrder;
  final int verseCount;
  bool isRead;

  Surah({
    required this.id,
    required this.surahNumber,
    required this.name,
    required this.englishName,
    required this.meaning,
    required this.revelationOrder,
    required this.verseCount,
    this.isRead = false,
  });

  factory Surah.fromJson(Map<String, dynamic> json) {
    return Surah(
      id: json['id'],
      surahNumber: json['surahNumber'],
      name: json['name'],
      englishName: json['englishName'],
      meaning: json['meaning'] ?? '',
      revelationOrder: json['revelationOrder'],
      verseCount: json['verseCount'],
      isRead: json['isRead'] ?? false,
    );
  }
}
