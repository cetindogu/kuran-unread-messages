import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'providers/surah_provider.dart';
import 'screens/surah_list_screen.dart';

void main() {
  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => SurahProvider()),
      ],
      child: const KuranApp(),
    ),
  );
}

class KuranApp extends StatelessWidget {
  const KuranApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Kuran Nüzul Sırası',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.teal),
        useMaterial3: true,
        appBarTheme: const AppBarTheme(
          backgroundColor: Colors.teal,
          foregroundColor: Colors.white,
        ),
      ),
      home: const SurahListScreen(),
    );
  }
}
