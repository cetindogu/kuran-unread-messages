class Verse {
  final int id;
  final int surahId;
  final int verseNumber;
  final int revelationOrder;
  final String arabicText;
  final String turkishTranslation;
  final DateTime? downloadedAt;
  bool isRead;

  Verse({
    required this.id,
    required this.surahId,
    required this.verseNumber,
    required this.revelationOrder,
    required this.arabicText,
    required this.turkishTranslation,
    this.downloadedAt,
    this.isRead = false,
  });

  factory Verse.fromJson(Map<String, dynamic> json) {
    return Verse(
      id: json['id'],
      surahId: json['surahId'],
      verseNumber: json['verseNumber'],
      revelationOrder: json['revelationOrder'],
      arabicText: json['arabicText'],
      turkishTranslation: json['turkishTranslation'],
      downloadedAt: json['downloadedAt'] != null
          ? DateTime.tryParse(json['downloadedAt'])
          : null,
      isRead: json['isRead'] ?? false,
    );
  }
}

class AIInterpretation {
  final int id;
  final int verseId;
  final String modelName;
  final String interpretation;
  final DateTime generatedAt;
  final int costTokens;

  AIInterpretation({
    required this.id,
    required this.verseId,
    required this.modelName,
    required this.interpretation,
    required this.generatedAt,
    this.costTokens = 0,
  });

  factory AIInterpretation.fromJson(Map<String, dynamic> json) {
    return AIInterpretation(
      id: json['id'],
      verseId: json['verseId'],
      modelName: json['modelName'],
      interpretation: json['interpretation'],
      generatedAt: DateTime.parse(json['generatedAt']),
      costTokens: json['costTokens'] ?? 0,
    );
  }
}

class LLMModel {
  final int id;
  final String modelName;
  final String displayName;
  final String providerName;
  final bool isFree;
  final bool hasApiKey;

  LLMModel({
    required this.id,
    required this.modelName,
    required this.displayName,
    required this.providerName,
    required this.isFree,
    this.hasApiKey = false,
  });

  factory LLMModel.fromJson(Map<String, dynamic> json) {
    return LLMModel(
      id: json['id'],
      modelName: json['modelName'],
      displayName: json['displayName'],
      providerName: json['providerName'],
      isFree: json['isFree'] ?? true,
      hasApiKey: json['hasApiKey'] ?? false,
    );
  }
}