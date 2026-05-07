import 'package:dio/dio.dart';
import '../models/surah.dart';
import '../models/verse.dart';

class ApiService {
  static const String _defaultBaseUrl = 'http://127.0.0.1:5286';
  
  final Dio _dio = Dio(BaseOptions(
    baseUrl: _defaultBaseUrl,
    connectTimeout: const Duration(seconds: 30),
    receiveTimeout: const Duration(seconds: 30),
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    },
  ));

  Future<List<Surah>> getSurahs({int? userId}) async {
    try {
      final response = await _dio.get('/Surahs', queryParameters: userId != null ? {'userId': userId} : {});
      if (response.statusCode == 200) {
        List<dynamic> data = response.data;
        return data.map((json) => Surah.fromJson(json)).toList();
      }
      throw Exception('Failed to load surahs');
    } catch (e) {
      print('Error fetching surahs: $e');
      rethrow;
    }
  }

  Future<List<Verse>> getSurahVerses(int surahNumber) async {
    try {
      final response = await _dio.get('/Surahs/$surahNumber/verses');
      if (response.statusCode == 200) {
        List<dynamic> data = response.data;
        return data.map((json) => Verse.fromJson(json)).toList();
      }
      throw Exception('Failed to load verses');
    } catch (e) {
      print('Error fetching verses: $e');
      rethrow;
    }
  }

  Future<void> markAsRead(int id, {int userId = 1}) async {
    try {
      await _dio.post('/Surahs/$id/markread', queryParameters: {'userId': userId});
    } catch (e) {
      print('Error marking surah as read: $e');
      rethrow;
    }
  }

  Future<void> markVerseAsRead(int verseId, {int userId = 1}) async {
    try {
      await _dio.post('/Verses/$verseId/markread', queryParameters: {'userId': userId});
    } catch (e) {
      print('Error marking verse as read: $e');
      rethrow;
    }
  }

  Future<List<LLMModel>> getModels() async {
    try {
      final response = await _dio.get('/LLM/models');
      if (response.statusCode == 200) {
        List<dynamic> data = response.data;
        return data.map((json) => LLMModel.fromJson(json)).toList();
      }
      throw Exception('Failed to load models');
    } catch (e) {
      print('Error fetching models: $e');
      rethrow;
    }
  }

  Future<List<AIInterpretation>> getVerseInterpretations(int verseId) async {
    try {
      final response = await _dio.get('/Verses/$verseId/interpretations');
      if (response.statusCode == 200) {
        List<dynamic> data = response.data;
        return data.map((json) => AIInterpretation.fromJson(json)).toList();
      }
      return [];
    } catch (e) {
      print('Error fetching interpretations: $e');
      return [];
    }
  }

  Future<AIInterpretation?> generateInterpretation(int verseId, int modelId) async {
    try {
      final response = await _dio.post('/Verses/$verseId/interpretations/$modelId/generate');
      if (response.statusCode == 200) {
        return AIInterpretation.fromJson(response.data);
      }
      return null;
    } catch (e) {
      print('Error generating interpretation: $e');
      return null;
    }
  }

  Future<void> setupFreeModels() async {
    try {
      await _dio.post('/LLM/setup-free-models');
    } catch (e) {
      print('Error setting up free models: $e');
      rethrow;
    }
  }

  Future<int> getUnreadCount({int userId = 1}) async {
    try {
      final response = await _dio.get('/notifications/unreadcount', queryParameters: {'userId': userId});
      if (response.statusCode == 200) {
        return response.data;
      }
      return 0;
    } catch (e) {
      print('Error fetching unread count: $e');
      return 0;
    }
  }

  Future<void> syncData() async {
    try {
      await _dio.post('/Verses/sync');
    } catch (e) {
      print('Error syncing data: $e');
      rethrow;
    }
  }

  Future<Map<String, dynamic>> runBatchInterpretation(int modelId, {int? surahId}) async {
    try {
      final response = await _dio.post('/LLM/batch-interpret', queryParameters: {
        'modelId': modelId,
        if (surahId != null) 'surahId': surahId,
      });
      return response.data;
    } catch (e) {
      print('Error running batch interpretation: $e');
      rethrow;
    }
  }
}