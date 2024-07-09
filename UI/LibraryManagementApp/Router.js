import { createRouter, createWebHistory } from 'vue-router';
import Home from './components/Home.vue'; // Assuming Home component exists
import About from './components/About.vue'; // Assuming About component exists
import Reviews from './components/Reviews.vue'; // Reviews component for displaying book reviews

const router = createRouter({
  history: createWebHistory(), // Using browser history API for routing
  routes: [
    { path: '/', component: Home }, // Home route
    { path: '/about', component: About }, // About route
    { path: '/Reviews/:bookId', component: Reviews, props: true }, // Reviews route with dynamic parameter
  ],
});

export default router;