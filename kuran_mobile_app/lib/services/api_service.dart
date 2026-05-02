import 'package:dio/dio.dart';
import '../models/surah.dart';
import '../models/verse.dart';

class ApiService {
  final Dio _dio = Dio(BaseOptions(
    baseUrl: 'http://localhost:5286', // Updated Backend Port
    connectTimeout: const Duration(seconds: 10),
    receiveTimeout: const Duration(seconds: 10),
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

  Future<int?> getUserId(String username) async {
    // Note: Backend doesn't have a direct login endpoint yet, but we added a test user in Seed.
    // For simplicity, we'll assume a fixed mapping or a simple check if we had an endpoint.
    if (username == 'test') return 2; // Test user id from Seed (default_user is 1)
    return null;
  }
}
